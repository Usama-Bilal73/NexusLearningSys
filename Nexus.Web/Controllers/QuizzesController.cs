using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nexus.Data.Identity;
using Nexus.Data.Models;
using Nexus.Data.Persistence;
using Nexus.Web.ViewModels.Quiz;

namespace Nexus.Web.Controllers;

[Authorize]
public class QuizzesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Services.IGradebookService _gradebookService;

    public QuizzesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Services.IGradebookService gradebookService)
    {
        _context = context;
        _userManager = userManager;
        _gradebookService = gradebookService;
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Index()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var quizzes = await _context.Quizzes.AsNoTracking().Include(q => q.Course).Include(q => q.Questions).Where(q => q.Course!.TeacherId == teacherId).OrderByDescending(q => q.Id).ToListAsync();
        return View(quizzes);
    }


    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> QuestionBank()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var questions = await _context.Questions.AsNoTracking()
            .Include(q => q.Quiz)!.ThenInclude(q => q!.Course)
            .Where(q => q.Quiz!.Course!.TeacherId == teacherId)
            .OrderBy(q => q.Quiz!.Course!.Name)
            .ThenBy(q => q.Quiz!.Title)
            .ToListAsync();
        return View(questions);
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Create() => View(await PopulateCoursesAsync(new QuizFormViewModel()));

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Create(QuizFormViewModel model)
    {
        if (!await OwnsCourseAsync(model.CourseId)) ModelState.AddModelError(nameof(model.CourseId), "Select one of your courses.");
        if (!ModelState.IsValid) return View(await PopulateCoursesAsync(model));
        _context.Quizzes.Add(new Quiz { CourseId = model.CourseId, Title = model.Title.Trim(), Description = model.Description, DurationMinutes = model.DurationMinutes, OpensAtUtc = model.OpensAtUtc, ClosesAtUtc = model.ClosesAtUtc, IsPublished = model.IsPublished });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Details(int id)
    {
        var quiz = await FindOwnedQuizAsync(id);
        return quiz is null ? NotFound() : View(quiz);
    }

    [Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> AddQuestion(int quizId)
    {
        if (await FindOwnedQuizAsync(quizId) is null) return NotFound();
        return View(new QuestionFormViewModel { QuizId = quizId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> AddQuestion(QuestionFormViewModel model)
    {
        if (await FindOwnedQuizAsync(model.QuizId) is null) return NotFound();
        if (!ModelState.IsValid) return View(model);
        _context.Questions.Add(new Question { QuizId = model.QuizId, Text = model.Text.Trim(), OptionA = model.OptionA.Trim(), OptionB = model.OptionB.Trim(), OptionC = model.OptionC.Trim(), OptionD = model.OptionD.Trim(), CorrectOption = model.CorrectOption, Points = model.Points });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = model.QuizId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Teacher)]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await FindOwnedQuizAsync(id);
        if (quiz is null) return NotFound();
        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> Available()
    {
        var studentId = _userManager.GetUserId(User)!;
        var now = DateTime.UtcNow;
        var quizzes = await _context.Quizzes.AsNoTracking().Include(q => q.Course).Include(q => q.Attempts)
            .Where(q => q.IsPublished && q.Course!.Enrollments.Any(e => e.StudentId == studentId) && (q.OpensAtUtc == null || q.OpensAtUtc <= now) && (q.ClosesAtUtc == null || q.ClosesAtUtc >= now))
            .OrderBy(q => q.Course!.Name).ToListAsync();
        return View(quizzes);
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> Take(int id)
    {
        var studentId = _userManager.GetUserId(User)!;
        var quiz = await FindAvailableQuizAsync(id, studentId);
        if (quiz is null) return NotFound();

        // Exam Security: enforce MaxAttempts
        if (quiz.MaxAttempts > 0)
        {
            var completedAttempts = await _context.QuizAttempts
                .CountAsync(a => a.QuizId == id && a.StudentId == studentId && a.SubmittedAtUtc != null);
            if (completedAttempts >= quiz.MaxAttempts)
            {
                TempData["Error"] = $"You have used all {quiz.MaxAttempts} attempt(s) for this quiz.";
                return RedirectToAction(nameof(Available));
            }
        }

        var attempt = await _context.QuizAttempts.Include(a => a.Answers).FirstOrDefaultAsync(a => a.QuizId == id && a.StudentId == studentId && a.SubmittedAtUtc == null);
        if (attempt is null)
        {
            attempt = new QuizAttempt { QuizId = id, StudentId = studentId, StartedAtUtc = DateTime.UtcNow };
            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();
        }
        var expires = attempt.StartedAtUtc.AddMinutes(quiz.DurationMinutes);
        var remaining = Math.Max(0, (int)(expires - DateTime.UtcNow).TotalSeconds);
        if (remaining == 0) return await SubmitAttempt(attempt.Id, true, new Dictionary<int, string?>());

        // Shuffle questions if enabled
        var questions = quiz.ShuffleQuestions
            ? quiz.Questions.OrderBy(_ => Guid.NewGuid()).ToList()
            : quiz.Questions.ToList();

        var completedCount = quiz.MaxAttempts > 0
            ? await _context.QuizAttempts.CountAsync(a => a.QuizId == id && a.StudentId == studentId && a.SubmittedAtUtc != null)
            : 0;
        var attemptsRemaining = quiz.MaxAttempts == 0 ? -1 : quiz.MaxAttempts - completedCount;

        return View(new QuizAttemptViewModel
        {
            QuizId           = quiz.Id,
            AttemptId        = attempt.Id,
            Title            = quiz.Title,
            RemainingSeconds = remaining,
            AttemptsRemaining = attemptsRemaining,
            Questions        = questions.Select(q => new QuestionAttemptViewModel { Id = q.Id, Text = q.Text, OptionA = q.OptionA, OptionB = q.OptionB, OptionC = q.OptionC, OptionD = q.OptionD }).ToList(),
            Answers          = attempt.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedOption)
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> Submit(QuizAttemptViewModel model) => await SubmitAttempt(model.AttemptId, false, model.Answers);

    private async Task<IActionResult> SubmitAttempt(int attemptId, bool autoSubmitted, Dictionary<int, string?> answers)
    {
        var studentId = _userManager.GetUserId(User)!;
        var attempt = await _context.QuizAttempts.Include(a => a.Quiz).ThenInclude(q => q!.Questions).Include(a => a.Answers).FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);
        if (attempt is null) return NotFound();
        if (attempt.SubmittedAtUtc is not null) return RedirectToAction(nameof(Result), new { id = attempt.Id });
        attempt.IsAutoSubmitted = autoSubmitted || DateTime.UtcNow >= attempt.StartedAtUtc.AddMinutes(attempt.Quiz!.DurationMinutes);
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        foreach (var question in attempt.Quiz.Questions)
        {
            answers.TryGetValue(question.Id, out var selected);
            var answer = new Answer { QuizAttemptId = attempt.Id, QuestionId = question.Id, SelectedOption = selected, IsCorrect = selected == question.CorrectOption, PointsEarned = selected == question.CorrectOption ? question.Points : 0 };
            attempt.Score += answer.PointsEarned;
            _context.Answers.Add(answer);
        }
        await _context.SaveChangesAsync();

        if (attempt.Quiz != null)
        {
            await _gradebookService.UpdateStudentQuizMarksAndTotalAsync(studentId, attempt.Quiz.CourseId);
        }

        return RedirectToAction(nameof(Result), new { id = attempt.Id });
    }

    [Authorize(Roles = ApplicationRoles.Student)]
    public async Task<IActionResult> Result(int id)
    {
        var studentId = _userManager.GetUserId(User)!;
        var attempt = await _context.QuizAttempts.AsNoTracking().Include(a => a.Quiz).ThenInclude(q => q!.Questions).Include(a => a.Answers).FirstOrDefaultAsync(a => a.Id == id && a.StudentId == studentId);
        return attempt is null ? NotFound() : View(attempt);
    }

    private async Task<Quiz?> FindOwnedQuizAsync(int id)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Quizzes.Include(q => q.Course).Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == id && q.Course!.TeacherId == teacherId);
    }

    private async Task<Quiz?> FindAvailableQuizAsync(int id, string studentId)
    {
        var now = DateTime.UtcNow;
        return await _context.Quizzes.Include(q => q.Questions).Include(q => q.Course).ThenInclude(c => c!.Enrollments).FirstOrDefaultAsync(q => q.Id == id && q.IsPublished && q.Course!.Enrollments.Any(e => e.StudentId == studentId) && (q.OpensAtUtc == null || q.OpensAtUtc <= now) && (q.ClosesAtUtc == null || q.ClosesAtUtc >= now));
    }

    private async Task<bool> OwnsCourseAsync(int courseId)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);
    }

    private async Task<QuizFormViewModel> PopulateCoursesAsync(QuizFormViewModel model)
    {
        var teacherId = _userManager.GetUserId(User)!;
        var courses = await _context.Courses.AsNoTracking().Where(c => c.TeacherId == teacherId).OrderBy(c => c.Name).ToListAsync();
        model.Courses = courses.Select(c => new SelectListItem($"{c.Name} ({c.Semester})", c.Id.ToString()));
        return model;
    }
}
