(function(){
    const signalrCdn = "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js";

    function loadScript(src){
        return new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = src;
            s.onload = resolve;
            s.onerror = reject;
            document.head.appendChild(s);
        });
    }

    async function init(){
        if (typeof signalR === 'undefined'){
            try{
                await loadScript(signalrCdn);
            }catch{
                console.warn('SignalR client failed to load. Real-time notifications unavailable.');
                return;
            }
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect()
            .build();

        connection.on('ReceiveNotification', payload => {
            try{
                // Update badge
                const badge = document.querySelector('.nav .fa-bell')?.closest('a')?.querySelector('.badge');
                if (badge){
                    let count = parseInt(badge.textContent || '0');
                    if (isNaN(count)) count = 0;
                    badge.textContent = (count + 1).toString();
                    badge.style.display = '';
                }

                // Show toast
                showToast(payload.title, payload.message);

            }catch(err){
                console.error('Error processing notification', err);
            }
        });

        connection.start().catch(err => console.error('SignalR connect error', err));

        function showToast(title, message){
            const container = document.getElementById('notificationToasts');
            if (!container) return;

            const toast = document.createElement('div');
            toast.className = 'toast align-items-center text-bg-white border-0';
            toast.setAttribute('role','alert');
            toast.setAttribute('aria-live','assertive');
            toast.setAttribute('aria-atomic','true');

            toast.innerHTML = `
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${escapeHtml(title)}</strong><div>${escapeHtml(message)}</div>
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            `;

            container.appendChild(toast);
            const bsToast = new bootstrap.Toast(toast, { delay: 8000 });
            bsToast.show();

            // remove after hidden
            toast.addEventListener('hidden.bs.toast', () => toast.remove());
        }

        function escapeHtml(unsafe){
            if (!unsafe) return '';
            return unsafe.replace(/[&<>"']/g, function(m){ return ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":"&#039;"})[m]; });
        }
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();
