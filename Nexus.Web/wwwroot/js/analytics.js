(function(){
    async function fetchJson(url){
        const res = await fetch(url);
        if(!res.ok) return [];
        return res.json();
    }

    function buildLineChart(ctx, labels, data, label){
        return new Chart(ctx, {
            type: 'line',
            data: { labels, datasets: [{ label, data, borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,0.08)', tension: 0.25 }] },
            options: { responsive: true, plugins: { legend: { display: false } } }
        });
    }

    function buildBarChart(ctx, labels, data, label){
        return new Chart(ctx, {
            type: 'bar',
            data: { labels, datasets: [{ label, data, backgroundColor: '#198754' }] },
            options: { responsive: true, plugins: { legend: { display: false } } }
        });
    }

    function buildDoughnut(ctx, labels, data){
        return new Chart(ctx, {
            type: 'doughnut',
            data: { labels, datasets: [{ data, backgroundColor: ['#0d6efd','#198754','#ffc107','#dc3545','#6f42c1'] }] },
            options: { responsive: true }
        });
    }

    async function init(){
        // Monthly Enrollments
        const m = await fetchJson('/Analytics/MonthlyEnrollments?months=12');
        const months = m.map(x=>x.label);
        const counts = m.map(x=>x.value);
        const ctx1 = document.getElementById('chartMonthlyEnrollments');
        if(ctx1) buildLineChart(ctx1.getContext('2d'), months, counts, 'Enrollments');

        // Course popularity
        const cp = await fetchJson('/Analytics/CoursePopularity?top=10');
        const courseLabels = cp.map(x=>x.courseName);
        const courseCounts = cp.map(x=>x.enrollmentCount);
        const ctx2 = document.getElementById('chartCoursePopularity');
        if(ctx2) buildBarChart(ctx2.getContext('2d'), courseLabels, courseCounts, 'Enrollments');

        // Assignment submission trends
        const asub = await fetchJson('/Analytics/AssignmentSubmissionTrends?months=12');
        const asLabels = asub.map(x=>x.label);
        const asCounts = asub.map(x=>x.value);
        const ctx3 = document.getElementById('chartAssignmentTrends');
        if(ctx3) buildLineChart(ctx3.getContext('2d'), asLabels, asCounts, 'Submissions');

        // Student performance
        const perf = await fetchJson('/Analytics/StudentPerformanceDistribution');
        const pLabels = perf.map(x=>x.label);
        const pCounts = perf.map(x=>x.value);
        const ctx4 = document.getElementById('chartPerformanceDistribution');
        if(ctx4) buildDoughnut(ctx4.getContext('2d'), pLabels, pCounts);
    }

    document.addEventListener('DOMContentLoaded', init);
})();
