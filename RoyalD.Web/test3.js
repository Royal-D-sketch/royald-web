<script>
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    document.querySelectorAll('input[name="method"]').forEach(r => {
        r.addEventListener('change', function () {
            document.getElementById('checkFields').classList.toggle('d-none', this.value !== 'Check');
            document.getElementById('uploadField').classList.toggle('d-none', this.value === 'Cash');
        });
    });

    const statusSelect = document.getElementById('statusSelect');
    if(statusSelect) {
        statusSelect.addEventListener('change', function() {
            const val = this.value;
            const ids = ['f-postponed', 'f-delivering', 'f-waiting', 'f-baddebt', 'f-return', 'f-file'];
            ids.forEach(id => {
                const el = document.getElementById(id);
                if(el) {
                    el.classList.add('d-none');
                    el.style.display = '';
                }
            });

            if (val === 'Postponed') document.getElementById('f-postponed').classList.remove('d-none');
            if (val === 'Delivering') document.getElementById('f-delivering').classList.remove('d-none');
            if (val === 'WaitingGoods') document.getElementById('f-waiting').classList.remove('d-none');
            if (val === 'BadDebt') { 
                document.getElementById('f-baddebt').classList.remove('d-none');
                document.getElementById('f-file').classList.remove('d-none');
            }
            if (val === 'ReturnIssued' || val === 'ReturnPending') {
                document.getElementById('f-return').classList.remove('d-none');
                document.getElementById('f-file').classList.remove('d-none');
                if (val === 'ReturnIssued') document.getElementById('ret2').click();
                else document.getElementById('ret1').click();
            }
            if (val === 'ChangeProduct') document.getElementById('f-file').classList.remove('d-none');
        });
    }

    document.querySelectorAll('input[name="returnType"]').forEach(r => {
        r.addEventListener('change', function() {
            if(this.value === 'Issued') document.getElementById('f-return-amount').classList.remove('d-none');
            else document.getElementById('f-return-amount').classList.add('d-none');
            if(statusSelect) statusSelect.value = this.value === 'Issued' ? 'ReturnIssued' : 'ReturnPending';
        });
    });
    
    if(statusSelect) {
        statusSelect.dispatchEvent(new Event('change'));
    }
    (function () {
        'use strict';
        const blackout = document.createElement('div');
        blackout.id = 'anti-capture-overlay';
        blackout.style.cssText = display:none; position:fixed; top:0; left:0; width:100vw; height:100vh;
            background:#000; z-index:2147483647; color:#fff;
            align-items:center; justify-content:center; flex-direction:column;
            font-size:1.5rem; font-family:'Sarabun',sans-serif; text-align:center;;
        blackout.innerHTML = <i class=""bi bi-shield-exclamation"" style=""font-size:5rem;color:#ff4444;""></i><br>
            <strong style=""font-size:1.8rem;"">🚨 ตรวจพบการแคปหน้าจอ</strong><br>
            <small style=""font-size:1rem;color:#aaa;"">ระบบกำลังบันทึกและออกจากระบบ...</small>;
        document.body.appendChild(blackout);

        let _loggedOut = false;
        function triggerBlackout(reason) {
            if (_loggedOut) return;
            _loggedOut = true;
            blackout.style.display = 'flex';
            document.body.style.overflow = 'hidden';
            const token = document.querySelector('input[name=""__RequestVerificationToken""]')?.value || '';
            fetch('/Account/ForceLogout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ reason: reason || 'SCREEN_CAPTURE_DETECTED' })
            }).finally(() => { setTimeout(() => { window.location.href = '/Account/Login?expired=1'; }, 1800); });
        }

        document.addEventListener('contextmenu', e => e.preventDefault());
        document.addEventListener('keydown', function (e) {
            const forbidden = ['c', 'a', 'p', 's', 'u'];
            if (e.ctrlKey && forbidden.includes(e.key.toLowerCase())) { e.preventDefault(); return; }
            if (e.key === 'PrintScreen' || e.code === 'PrintScreen') {
                e.preventDefault();
                triggerBlackout('PRINTSCREEN_KEY');
            }
            if (e.shiftKey && e.metaKey && (e.key === 'S' || e.key === 's')) { e.preventDefault(); triggerBlackout('WIN_SNIPTOOL'); }
        });
        document.addEventListener('visibilitychange', function () {
            if (document.visibilityState === 'hidden') {
                document.querySelectorAll('.card-body, table').forEach(el => { el.dataset._savedVis = el.style.visibility || 'visible'; el.style.visibility = 'hidden'; });
            } else {
                document.querySelectorAll('.card-body, table').forEach(el => { if(el.dataset._savedVis) el.style.visibility = el.dataset._savedVis; });
            }
        });
    })();
</script>
