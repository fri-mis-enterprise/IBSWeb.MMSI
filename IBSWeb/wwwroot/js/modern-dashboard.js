/**
 * modern-dashboard.js
 * JavaScript controller for the new MSAP Modern Dashboard.
 * Fetches operational counts, monthly financial trends, and recent activity via AJAX.
 * Handles chart rendering via ApexCharts and runs auto-refresh polling.
 */

(function () {
    'use strict';

    // Auto-refresh interval (5 minutes)
    const REFRESH_INTERVAL = 5 * 60 * 1000;
    let refreshTimer = null;

    // Currency Formatter
    const currencyFormatter = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'PHP',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    // Chart instance
    let trendChart = null;

    /**
     * Initialize Dashboard
     */
    function init() {
        if (!document.querySelector('.modern-dashboard-view')) return;

        // Set date
        updateBannerDate();

        // Fetch initial data
        fetchDashboardData();

        // Start polling if body has modern-ui enabled
        startPolling();

        // Watch for theme changes to update chart colors
        const themeObserver = new MutationObserver(function() {
            if (trendChart) {
                var el = document.querySelector('#trend-chart');
                if (el && el.innerHTML) {
                    var data = trendChart.w.config.series;
                    var categories = trendChart.w.config.xaxis.categories;
                    trendChart.updateOptions(chartOptions({
                        monthlyTrends: categories.map(function(c, i) {
                            return { monthName: c.split(' ')[0], year: c.split(' ')[1], billingAmount: data[0].data[i], collectionAmount: data[1].data[i] };
                        })
                    }));
                }
            }
        });
        themeObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });
    }

    /**
     * Set Banner Date
     */
    function updateBannerDate() {
        const dateEl = document.getElementById('modernCurrentDate');
        if (dateEl) {
            dateEl.textContent = new Date().toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
        }
    }

    /**
     * Start data polling
     */
    function startPolling() {
        if (refreshTimer) clearInterval(refreshTimer);
        refreshTimer = setInterval(fetchDashboardData, REFRESH_INTERVAL);
    }

    /**
     * Stop data polling
     */
    function stopPolling() {
        if (refreshTimer) {
            clearInterval(refreshTimer);
            refreshTimer = null;
        }
    }

    /**
     * Fetch Dashboard Data via AJAX
     */
    function fetchDashboardData() {
        // Show lightweight loader or skip to avoid flicker
        $.ajax({
            url: '/User/Home/GetDashboardData',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                renderDashboard(data);
            },
            error: function (xhr, status, error) {
                console.error("Failed to load dashboard data:", error);
            }
        });
    }

    /**
     * Render all dashboard components
     */
    function renderDashboard(data) {
        // 1. Render KPI values & counts
        updateKpiValues(data);

        // 2. Render Trend Chart
        if (data.monthlyTrends && data.monthlyTrends.length > 0) {
            renderChart(data.monthlyTrends);
        }

        // 4. Render Recent Activity
        if (data.recentActivity) {
            renderActivityFeed(data.recentActivity);
        }
    }

    /**
     * Update KPI Cards content
     */
    function updateKpiValues(data) {
        // Job Orders
        $('#jo-total-value').text(data.jobOrders.total.toLocaleString());
        $('#jo-open-count').text(data.jobOrders.open.toLocaleString());
        $('#jo-closed-count').text(data.jobOrders.closed.toLocaleString());

        // Dispatch Tickets
        $('#dt-total-value').text(data.dispatchTickets.total.toLocaleString());
        $('#dt-tariff-count').text(data.dispatchTickets.forTariff.toLocaleString());
        $('#dt-approval-count').text(data.dispatchTickets.forApproval.toLocaleString());
        $('#dt-disapproved-count').text(data.dispatchTickets.disapproved.toLocaleString());
        $('#dt-billing-count').text(data.dispatchTickets.forBilling.toLocaleString());
        $('#dt-billed-count').text(data.dispatchTickets.billed.toLocaleString());

        // Billings
        $('#b-total-value').text(data.billings.total.toLocaleString());
        $('#b-posting-count').text(data.billings.forPosting.toLocaleString());
        $('#b-collection-count').text(data.billings.forCollection.toLocaleString());
        $('#b-collected-count').text(data.billings.collected.toLocaleString());

        // Collections
        $('#c-total-value').text(data.collections.total.toLocaleString());
        $('#c-active-count').text(data.collections.active.toLocaleString());
        $('#c-voided-count').text(data.collections.voided.toLocaleString());
    }

    /**
     * Read a CSS custom property value from :root
     */
    function cssVar(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    /**
     * Build chart options with current theme colors
     */
    function chartOptions(trends) {
        var categories = trends.map(function(t) { return t.monthName + ' ' + t.year; });
        var billingAmounts = trends.map(function(t) { return t.billingAmount; });
        var collectionAmounts = trends.map(function(t) { return t.collectionAmount; });
        var textColor = cssVar('--on-surface') || '#1e293b';
        var gridColor = cssVar('--outline-variant') || '#e2e8f0';
        var tooltipBg = cssVar('--surface-container-lowest') || '#ffffff';
        var tooltipText = cssVar('--on-surface') || '#1e293b';

        return {
            series: [
                { name: 'Billed Amount', data: billingAmounts },
                { name: 'Collected Amount', data: collectionAmounts }
            ],
            chart: {
                type: 'line',
                height: 350,
                toolbar: { show: false },
                zoom: { enabled: false },
                fontFamily: 'Inter, system-ui, sans-serif',
                foreColor: textColor
            },
            colors: ['#0059bb', '#10b981'],
            stroke: { width: 3, curve: 'smooth' },
            markers: { size: 4, strokeWidth: 0, hover: { size: 6 } },
            xaxis: {
                categories: categories,
                axisBorder: { show: false },
                axisTicks: { show: false },
                labels: { style: { colors: textColor } }
            },
            yaxis: {
                labels: {
                    style: { colors: textColor },
                    formatter: function (value) {
                        return '₱' + value.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
                    }
                }
            },
            tooltip: {
                theme: document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light',
                y: {
                    formatter: function (value) {
                        return currencyFormatter.format(value);
                    }
                }
            },
            grid: { borderColor: gridColor },
            legend: {
                position: 'top',
                horizontalAlign: 'right',
                offsetY: -10,
                labels: { colors: textColor }
            }
        };
    }

    /**
     * Render Trend Chart using ApexCharts, theme-aware
     */
    function renderChart(trends) {
        var options = chartOptions(trends);

        if (trendChart) {
            trendChart.updateOptions(options);
        } else {
            trendChart = new ApexCharts(document.querySelector("#trend-chart"), options);
            trendChart.render();
        }
    }

    /**
     * Render Recent Activity Feed
     */
    function renderActivityFeed(activities) {
        const feed = $('#activity-feed-list');
        feed.empty();

        if (activities.length === 0) {
            feed.append('<div class="text-center text-muted py-4">No recent activity.</div>');
            return;
        }

        activities.forEach(act => {
            // Get user initials for avatar
            const initials = act.user ? act.user.substring(0, 2).toUpperCase() : 'SYS';
            
            // Badge class mapping
            let badgeClass = 'bg-secondary text-white';
            if (act.status === 'Open' || act.status === 'Requested' || act.status === 'For Tariff') {
                badgeClass = 'bg-primary text-white';
            } else if (act.status === 'Billed' || act.status === 'Collected') {
                badgeClass = 'bg-success text-white';
            } else if (act.status === 'Disapproved') {
                badgeClass = 'bg-danger text-white';
            }

            const item = $(`
                <div class="activity-item">
                    <div class="activity-avatar" title="${act.user || 'System'}">
                        ${initials}
                    </div>
                    <div class="activity-details">
                        <div class="activity-header-row">
                            <p class="activity-desc">
                                Created <strong>${act.type} #${act.number}</strong>
                            </p>
                            <span class="activity-badge ${badgeClass}">${act.status}</span>
                        </div>
                        <div class="activity-meta">
                            <span class="activity-user">By: ${act.user || 'System'}</span>
                            <span class="activity-date">Date: ${act.dateFormatted}</span>
                            <div class="activity-time">
                                <span class="material-symbols-outlined">schedule</span>
                                <span>${act.timeFormatted}</span>
                            </div>
                        </div>
                    </div>
                </div>
            `);
            feed.append(item);
        });
    }

    // Initialize on DOM load
    $(document).ready(init);
})();
