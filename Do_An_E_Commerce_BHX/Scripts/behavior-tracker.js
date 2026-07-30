/**
 * BÁCH HÓA XANH - BEHAVIOR TRACKER ENGINE v1.0
 * Tự động ghi nhận Dwell Time, Scroll Depth, Rage Click, Search & Funnel Events
 */
(function () {
    'use strict';

    // 1. Tạo hoặc lấy Session ID cố định cho phiên truy cập
    function getSessionId() {
        var sid = sessionStorage.getItem('bhx_sid');
        if (!sid) {
            sid = 'sid_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now();
            sessionStorage.setItem('bhx_sid', sid);
        }
        return sid;
    }

    var sessionId = getSessionId();
    var pageStartTime = Date.now();
    var activeDwellSeconds = 0;
    var lastActiveTimestamp = Date.now();
    var isTabActive = true;
    var loggedScrollDepths = {};

    // 2. Gửi dữ liệu ngầm (Beacon API hoặc fetch)
    function sendLog(data) {
        data.SessionId = sessionId;
        data.ReferrerUrl = data.ReferrerUrl || document.referrer;
        data.DeviceType = window.innerWidth <= 768 ? 'Mobile' : (window.innerWidth <= 1024 ? 'Tablet' : 'Desktop');

        var url = '/Analytics/LogEvent';
        var payload = JSON.stringify(data);

        if (navigator.sendBeacon) {
            var blob = new Blob([payload], { type: 'application/json' });
            navigator.sendBeacon(url, blob);
        } else {
            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: payload,
                keepalive: true
            }).catch(function () { });
        }
    }

    // Export API toàn cục cho các view gọi thủ công
    window.BHXTracker = {
        logEvent: function (eventType, targetId, targetName, extraData) {
            sendLog({
                EventType: eventType,
                TargetId: targetId || null,
                TargetName: targetName || '',
                ExtraDataJson: typeof extraData === 'object' ? JSON.stringify(extraData) : (extraData || '')
            });
        }
    };

    // Tự động ghi nhận lượt truy cập trang (PageView) ngay khi mở web
    try {
        sendLog({
            EventType: 'PageView',
            TargetName: window.location.pathname || '/'
        });
    } catch (e) { }

    // 3. TỰ ĐỘNG ĐO THỜI GIAN LƯU LẠI TRANG (PAGE DWELL TIME) & VISIBILITY API
    function updateActiveDwellTime() {
        if (isTabActive) {
            var now = Date.now();
            activeDwellSeconds += Math.floor((now - lastActiveTimestamp) / 1000);
            lastActiveTimestamp = now;
        }
    }

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
            updateActiveDwellTime();
            isTabActive = false;
            sendDwellTimeLog();
        } else {
            isTabActive = true;
            lastActiveTimestamp = Date.now();
        }
    });

    function sendDwellTimeLog() {
        if (activeDwellSeconds > 0) {
            sendLog({
                EventType: 'PageDwellTime',
                TargetName: window.location.pathname,
                DurationSeconds: activeDwellSeconds
            });
        }
    }

    window.addEventListener('beforeunload', function () {
        updateActiveDwellTime();
        sendDwellTimeLog();
    });

    // 4. TỰ ĐỘNG ĐO TỐC ĐỘ TẢI TRANG (PAGE LOAD SPEED)
    window.addEventListener('load', function () {
        setTimeout(function () {
            var loadMs = 0;
            if (window.performance && window.performance.timing) {
                loadMs = window.performance.timing.loadEventEnd - window.performance.timing.navigationStart;
            }
            if (loadMs > 0) {
                sendLog({
                    EventType: 'PageLoadSpeed',
                    TargetName: window.location.pathname,
                    PageLoadMs: loadMs
                });
            }
        }, 500);
    });

    // 5. TỰ ĐỘNG ĐO ĐỘ CUỘN TRANG (SCROLL DEPTH TRACKING 25%, 50%, 75%, 100%)
    function checkScrollDepth() {
        var winHeight = window.innerHeight;
        var docHeight = document.documentElement.scrollHeight - winHeight;
        if (docHeight <= 0) return;

        var scrollTop = window.scrollY || window.pageYOffset || document.documentElement.scrollTop;
        var scrollPercent = Math.round((scrollTop / docHeight) * 100);

        var thresholds = [25, 50, 75, 100];
        thresholds.forEach(function (t) {
            if (scrollPercent >= t && !loggedScrollDepths[t]) {
                loggedScrollDepths[t] = true;
                sendLog({
                    EventType: 'ScrollDepth',
                    TargetName: window.location.pathname,
                    ScrollPercent: t
                });
            }
        });
    }

    var scrollDebounceTimer;
    window.addEventListener('scroll', function () {
        clearTimeout(scrollDebounceTimer);
        scrollDebounceTimer = setTimeout(checkScrollDepth, 200);
    });

    // 6. TỰ ĐỘNG PHÁT HIỆN CÚ NHẤP BỰC BỘI (RAGE CLICKS)
    var clickHistory = [];
    document.addEventListener('click', function (e) {
        var now = Date.now();
        clickHistory.push({ time: now, x: e.clientX, y: e.clientY });

        // Giữ tối đa 5 click gần nhất
        if (clickHistory.length > 5) clickHistory.shift();

        if (clickHistory.length >= 3) {
            var first = clickHistory[0];
            var last = clickHistory[clickHistory.length - 1];
            var timeDiff = last.time - first.time;
            var dist = Math.hypot(last.x - first.x, last.y - first.y);

            // 3+ clicks trong 800ms ở cùng vị trí (< 30px) -> Rage Click
            if (timeDiff <= 800 && dist < 30) {
                var targetText = (e.target.innerText || e.target.tagName || '').substring(0, 50);
                sendLog({
                    EventType: 'RageClick',
                    TargetName: targetText,
                    ExtraDataJson: JSON.stringify({ path: window.location.pathname, x: e.clientX, y: e.clientY })
                });
                clickHistory = []; // Reset sau khi log
            }
        }
    }, true);

})();
