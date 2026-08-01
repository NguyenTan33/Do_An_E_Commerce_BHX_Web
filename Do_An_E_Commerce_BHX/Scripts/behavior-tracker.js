/**
 * BÁCH HÓA XANH - BEHAVIOR TRACKER ENGINE v4.0 (Tối ưu hóa Modal Chi Tiết & searchTerm)
 * Tự động ghi nhận Dwell Time, Scroll Depth, Rage Click, Search & Funnel Events
 */
(function () {
    'use strict';

    function getSessionId() {
        var sid = sessionStorage.getItem('bhx_sid');
        if (!sid) {
            sid = 'sid_' + Math.random().toString(36).substring(2, 11) + '_' + Date.now();
            sessionStorage.setItem('bhx_sid', sid);
        }
        return sid;
    }

    var sessionId = getSessionId();
    var isTabActive = true;
    var loggedScrollDepths = {};

    function sendLog(data) {
        data.SessionId = sessionId;
        data.ReferrerUrl = data.ReferrerUrl || document.referrer;
        data.DeviceType = window.innerWidth <= 768 ? 'Mobile' : (window.innerWidth <= 1024 ? 'Tablet' : 'Desktop');

        var url = '/Analytics/LogEvent';
        var payload = JSON.stringify(data);

        if (window.jQuery) {
            window.jQuery.ajax({
                url: url,
                type: 'POST',
                data: payload,
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                async: true
            });
        } else if (navigator.sendBeacon) {
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

    var pathname = window.location.pathname || '/';
    var urlParams = new URLSearchParams(window.location.search);

    // TRÍCH XUẤT THÔNG TIN SẢN PHẨM (HỖ TRỢ CẢ TRANG CHI TIẾT LẪN POPUP MODAL TẠI TRANG CHỦ)
    function getProductInfo() {
        // 1. Ưu tiên kiểm tra Modal Chi Tiết đang mở trên giao diện
        var modalId = window.BHX_CURRENT_MODAL_PRODUCT_ID;
        if (!modalId && window.jQuery && window.jQuery('#pmd-current-id').length > 0) {
            var val = window.jQuery('#pmd-current-id').val();
            if (val && !isNaN(parseInt(val))) modalId = parseInt(val);
        }
        if (modalId && window.jQuery && window.jQuery('#homeProductDetailModal').is(':visible')) {
            var modalName = window.BHX_CURRENT_MODAL_PRODUCT_NAME || (window.jQuery('#pmd-name').text()) || ('Sản phẩm #' + modalId);
            return { id: modalId, name: modalName };
        }

        // 2. Kiểm tra trang chi tiết thuần (/Product/Detail)
        var pid = window.BHX_PRODUCT_ID || null;
        var pname = window.BHX_PRODUCT_NAME || '';

        if (!pid) {
            var qId = urlParams.get('productId') || urlParams.get('id');
            if (qId && !isNaN(parseInt(qId))) pid = parseInt(qId);
        }
        if (!pid) {
            var m = pathname.match(/\/Product\/Detail\/(\d+)/i) || (window.location.href).match(/productId=(\d+)/i);
            if (m) pid = parseInt(m[1]);
        }
        if (pid && !pname && document.title) {
            pname = document.title.split('-')[0].trim();
        }
        return { id: pid, name: pname };
    }

    try {
        // Ghi nhận lượt xem trang (PageView)
        sendLog({
            EventType: 'PageView',
            TargetName: pathname + window.location.search
        });

        // Ghi nhận ViewProduct
        var pInfo = getProductInfo();
        if (pInfo.id) {
            sendLog({
                EventType: 'ViewProduct',
                TargetId: pInfo.id,
                TargetName: pInfo.name || ('Sản phẩm #' + pInfo.id)
            });
        }

        // TỰ ĐỘNG BẮT TỪ KHÓA TÌM KIẾM (TRÍCH XUẤT CHÍNH XÁC THAM SỐ searchTerm)
        var q = urlParams.get('searchTerm') || urlParams.get('searchName') || urlParams.get('searchKey') || urlParams.get('tuKhoa') || urlParams.get('q') || urlParams.get('search') || urlParams.get('keyword');
        if (q && q.trim().length > 0) {
            sendLog({
                EventType: 'SearchKeyword',
                TargetName: q.trim()
            });
        }

        // Ghi nhận CheckoutStarted
        if (pathname.indexOf('/Order') >= 0 || pathname.indexOf('/Payment') >= 0) {
            sendLog({
                EventType: 'CheckoutStarted',
                TargetName: pathname
            });
        }
    } catch (e) { }

    // HEARTBEAT PING 5s TÍCH LŨY THỜI GIAN ĐỌC CHI TIẾT SẢN PHẨM & MODAL
    setInterval(function () {
        if (isTabActive) {
            var pInfo = getProductInfo();
            sendLog({
                EventType: 'PageDwellTime',
                TargetId: pInfo.id,
                TargetName: pInfo.id ? ('/Product/Detail/' + pInfo.id) : pathname,
                DurationSeconds: 5
            });
        }
    }, 5000);

    document.addEventListener('visibilitychange', function () {
        isTabActive = (document.visibilityState === 'visible');
    });

    // Tốc độ tải trang
    window.addEventListener('load', function () {
        setTimeout(function () {
            var loadMs = 0;
            if (window.performance && window.performance.timing) {
                loadMs = window.performance.timing.loadEventEnd - window.performance.timing.navigationStart;
            }
            if (loadMs > 0) {
                sendLog({
                    EventType: 'PageLoadSpeed',
                    TargetName: pathname,
                    PageLoadMs: loadMs
                });
            }
        }, 500);
    });

    // Độ cuộn trang (Scroll Depth)
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
                    TargetName: pathname,
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

    // Nút thêm vào giỏ hàng
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-add-to-cart, .btn-add-cart, [data-action="add-cart"]');
        if (btn) {
            var pInfo = getProductInfo();
            var prodId = btn.getAttribute('data-product-id') || btn.getAttribute('data-id') || pInfo.id;
            var prodName = btn.getAttribute('data-product-name') || btn.getAttribute('data-name') || pInfo.name || 'Sản phẩm';
            sendLog({
                EventType: 'AddToCart',
                TargetId: prodId ? parseInt(prodId) : null,
                TargetName: prodName
            });
        }
    }, true);

    // Rage Clicks
    var clickHistory = [];
    document.addEventListener('click', function (e) {
        var now = Date.now();
        clickHistory.push({ time: now, x: e.clientX, y: e.clientY });

        if (clickHistory.length > 5) clickHistory.shift();

        if (clickHistory.length >= 3) {
            var first = clickHistory[0];
            var last = clickHistory[clickHistory.length - 1];
            var timeDiff = last.time - first.time;
            var dist = Math.hypot(last.x - first.x, last.y - first.y);

            if (timeDiff <= 800 && dist < 30) {
                var targetText = (e.target.innerText || e.target.tagName || '').substring(0, 50);
                sendLog({
                    EventType: 'RageClick',
                    TargetName: targetText,
                    ExtraDataJson: JSON.stringify({ path: pathname, x: e.clientX, y: e.clientY })
                });
                clickHistory = [];
            }
        }
    }, true);

})();
