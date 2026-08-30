using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string path = ""../RoyalD.Web/Views/Debtor/Detail.cshtml"";
        string file = File.ReadAllText(path);
        string pattern = @""\(function \(\) \{.*?</script>"";
        string replacement = ""(function () { \n"" +
""        'use strict'; \n"" +
""        const blackout = document.createElement('div'); \n"" +
""        blackout.id = 'anti-capture-overlay'; \n"" +
""        blackout.style.cssText = display:none; position:fixed; top:0; left:0; width:100vw; height:100vh; background:#000; z-index:2147483647; color:#fff; align-items:center; justify-content:center; flex-direction:column; font-size:1.5rem; font-family:'Sarabun',sans-serif; text-align:center;; \n"" +
""        blackout.innerHTML = <i class=\""bi bi-shield-exclamation\"" style=\""font-size:5rem;color:#ff4444;\""></i><br><strong style=\""font-size:1.8rem;\"">🚨 ตรวจพบการแคปหน้าจอ</strong><br><small style=\""font-size:1rem;color:#aaa;\"">ระบบกำลังบันทึกและออกจากระบบ...</small>; \n"" +
""        document.body.appendChild(blackout); \n"" +
""        let _loggedOut = false; \n"" +
""        function triggerBlackout(reason) { \n"" +
""            if (_loggedOut) return; \n"" +
""            _loggedOut = true; \n"" +
""            blackout.style.display = 'flex'; \n"" +
""            document.body.style.overflow = 'hidden'; \n"" +
""            const token = document.querySelector('input[name=\""__RequestVerificationToken\""]')?.value || ''; \n"" +
""            fetch('/Account/ForceLogout', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ reason: reason || 'SCREEN_CAPTURE_DETECTED' }) }).finally(() => { setTimeout(() => { window.location.href = '/Account/Login?expired=1'; }, 1800); }); \n"" +
""        } \n"" +
""        document.addEventListener('contextmenu', e => e.preventDefault()); \n"" +
""        document.addEventListener('keydown', function (e) { \n"" +
""            const forbidden = ['c', 'a', 'p', 's', 'u']; \n"" +
""            if (e.ctrlKey && forbidden.includes(e.key.toLowerCase())) { e.preventDefault(); return; } \n"" +
""            if (e.key === 'PrintScreen' || e.code === 'PrintScreen') { e.preventDefault(); triggerBlackout('PRINTSCREEN_KEY'); } \n"" +
""            if (e.shiftKey && e.metaKey && (e.key === 'S' || e.key === 's')) { e.preventDefault(); triggerBlackout('WIN_SNIPTOOL'); } \n"" +
""        }); \n"" +
""        document.addEventListener('visibilitychange', function () { \n"" +
""            if (document.visibilityState === 'hidden') { \n"" +
""                document.querySelectorAll('.card-body, table').forEach(el => { el.dataset._savedVis = el.style.visibility || 'visible'; el.style.visibility = 'hidden'; }); \n"" +
""            } else { \n"" +
""                document.querySelectorAll('.card-body, table').forEach(el => { if(el.dataset._savedVis) el.style.visibility = el.dataset._savedVis; }); \n"" +
""            } \n"" +
""        }); \n"" +
""    })(); \n"" +
""</script>"";
        file = Regex.Replace(file, pattern, replacement, RegexOptions.Singleline);
        File.WriteAllText(path, file);
    }
}
