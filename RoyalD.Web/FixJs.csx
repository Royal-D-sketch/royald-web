using System.IO;
using System.Text.RegularExpressions;

string file = File.ReadAllText(""Views/Debtor/Detail.cshtml"");
string pattern = @""\(function \(\) \{.*?</script>"";
string replacement = @""(function () {
        'use strict';
        const blackout = document.createElement('div');
        blackout.id = 'anti-capture-overlay';
        blackout.style.cssText = display:none; position:fixed; top:0; left:0; width:100vw; height:100vh;
            background:#000; z-index:2147483647; color:#fff;
            align-items:center; justify-content:center; flex-direction:column;
            font-size:1.5rem; font-family:'Sarabun',sans-serif; text-align:center;;
        blackout.innerHTML = <i class=\""bi bi-shield-exclamation\"" style=\""font-size:5rem;color:#ff4444;\""></i><br>
            <strong style=\""font-size:1.8rem;\"">🚨 ตรวจพบการแคปหน้าจอ</strong><br>
            <small style=\""font-size:1rem;color:#aaa;\"">ระบบกำลังบันทึกและออกจากระบบ...</small>;
        document.body.appendChild(blackout);

        let _loggedOut = false;
        function triggerBlackout(reason) {
            if (_loggedOut) return;
            _loggedOut = true;
            blackout.style.display = 'flex';
            document.body.style.overflow = 'hidden';
            const token = document.querySelector('input[name=\""__RequestVerificationToken\""]')?.value || '';
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
</script>"";

file = Regex.Replace(file, pattern, replacement, RegexOptions.Singleline);
File.WriteAllText(""Views/Debtor/Detail.cshtml"", file);
