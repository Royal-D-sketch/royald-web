/**
 * Anti Screen Capture Script — RoyalD Debtor System
 * บล็อกการ PrintScreen / คัดลอก / Screenshot บนหน้า SalesBill Detail และ Debtor Detail
 * Version 2.0
 */
(function () {
    'use strict';

    var blackoutActive = false;

    // ===== สร้าง Blackout Overlay =====
    var overlay = document.createElement('div');
    overlay.id = '_royald_blackout';
    overlay.style.cssText = [
        'display:none',
        'position:fixed',
        'top:0', 'left:0',
        'width:100vw', 'height:100vh',
        'background:#000000',
        'z-index:2147483647',
        'color:#fff',
        'align-items:center',
        'justify-content:center',
        'flex-direction:column',
        'text-align:center',
        'font-family:Sarabun,sans-serif',
        'font-size:20px'
    ].join(';');
    overlay.innerHTML = [
        '<div style="font-size:80px;">🛡️</div>',
        '<div style="font-size:2rem;font-weight:bold;color:#ff4444;">⚠ ตรวจพบการแคปหน้าจอ</div>',
        '<div style="font-size:1rem;color:#aaa;margin-top:12px;">ระบบกำลังบันทึกเหตุการณ์และออกจากระบบ...</div>',
        '<div style="font-size:0.85rem;color:#888;margin-top:8px;">กรุณาติดต่อผู้ดูแลระบบ</div>'
    ].join('');
    document.body.appendChild(overlay);

    function activateBlackout(reason) {
        if (blackoutActive) return;
        blackoutActive = true;

        // แสดง overlay สีดำ
        overlay.style.display = 'flex';
        document.body.style.overflow = 'hidden';

        // Force logout
        var token = (document.querySelector('input[name="__RequestVerificationToken"]') || {}).value || '';
        fetch('/Account/ForceLogout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ reason: 'SCREEN_CAPTURE: ' + reason })
        }).finally(function () {
            setTimeout(function () {
                window.location.href = '/Account/Login?expired=1';
            }, 2000);
        });
    }

    // 1. บล็อกคลิกขวา
    document.addEventListener('contextmenu', function (e) {
        e.preventDefault();
        return false;
    });

    // 2. บล็อก copy / cut
    document.addEventListener('copy', function (e) { e.preventDefault(); return false; });
    document.addEventListener('cut', function (e) { e.preventDefault(); return false; });

    // 3. ดักจับ Keyboard shortcuts
    document.addEventListener('keydown', function (e) {
        // PrintScreen (keyCode 44)
        if (e.keyCode === 44 || e.key === 'PrintScreen' || e.code === 'PrintScreen') {
            e.preventDefault();
            navigator.clipboard && navigator.clipboard.writeText('').catch(function () {});
            activateBlackout('PrintScreen key');
            return false;
        }

        // Windows Snipping Tool: Win+Shift+S (metaKey)
        if (e.metaKey && e.shiftKey && (e.key === 'S' || e.key === 's')) {
            e.preventDefault();
            activateBlackout('Win+Shift+S Snipping Tool');
            return false;
        }

        // Ctrl+P (print) — blackout เพราะ print จากหน้านี้ไม่ได้รับอนุญาต
        if (e.ctrlKey && (e.key === 'p' || e.key === 'P')) {
            e.preventDefault();
            return false;
        }

        // Ctrl+S (save page)
        if (e.ctrlKey && (e.key === 's' || e.key === 'S')) {
            e.preventDefault();
            return false;
        }

        // Ctrl+C / Ctrl+A / Ctrl+U
        if (e.ctrlKey && ['c', 'a', 'u', 'C', 'A', 'U'].indexOf(e.key) !== -1) {
            e.preventDefault();
            return false;
        }

        // F12 (DevTools)
        if (e.keyCode === 123) {
            e.preventDefault();
            return false;
        }

        // Ctrl+Shift+I (DevTools)
        if (e.ctrlKey && e.shiftKey && (e.key === 'I' || e.key === 'i')) {
            e.preventDefault();
            return false;
        }
    });

    // 4. ซ่อนข้อมูลชั่วคราวเมื่อ tab ถูกซ่อน
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
            var els = document.querySelectorAll('.bill-section, .card-body, table');
            for (var i = 0; i < els.length; i++) {
                els[i]._savedVis = els[i].style.visibility;
                els[i].style.visibility = 'hidden';
            }
        } else {
            var els2 = document.querySelectorAll('.bill-section, .card-body, table');
            for (var j = 0; j < els2.length; j++) {
                els2[j].style.visibility = els2[j]._savedVis || '';
            }
        }
    });

    // 5. ดักจับ Screen Share API (Chrome)
    if (navigator.mediaDevices && navigator.mediaDevices.getDisplayMedia) {
        var _orig = navigator.mediaDevices.getDisplayMedia.bind(navigator.mediaDevices);
        navigator.mediaDevices.getDisplayMedia = function (constraints) {
            activateBlackout('getDisplayMedia (Screen Share) API');
            return _orig(constraints);
        };
    }

    // 6. บล็อก user-select ด้วย CSS
    var noSelectStyle = document.createElement('style');
    noSelectStyle.textContent = [
        '.bill-section, .card-body, .table, code, .fw-bold {',
        '  user-select: none !important;',
        '  -webkit-user-select: none !important;',
        '  -moz-user-select: none !important;',
        '}'
    ].join('\n');
    document.head.appendChild(noSelectStyle);

    console.log('[RoyalD AntiCapture v2.0] Protection active.');
})();
