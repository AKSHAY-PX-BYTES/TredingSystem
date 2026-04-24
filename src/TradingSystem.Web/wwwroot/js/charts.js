// Chart.js helper functions for Blazor interop
window.chartInterop = {
    charts: {},

    createLineChart: function (canvasId, labels, datasets, title) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        // Destroy existing chart if any
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        const chartDatasets = datasets.map((ds, index) => {
            const colors = [
                'rgb(59, 130, 246)',   // blue
                'rgb(239, 68, 68)',    // red
                'rgb(34, 197, 94)',    // green
                'rgb(234, 179, 8)',    // yellow
                'rgb(168, 85, 247)',   // purple
                'rgb(236, 72, 153)',   // pink
            ];
            return {
                label: ds.label,
                data: ds.data,
                borderColor: ds.color || colors[index % colors.length],
                backgroundColor: (ds.color || colors[index % colors.length]).replace('rgb', 'rgba').replace(')', ', 0.1)'),
                borderWidth: 2,
                fill: ds.fill || false,
                tension: 0.3,
                pointRadius: ds.pointRadius !== undefined ? ds.pointRadius : 1,
                pointHoverRadius: 5,
            };
        });

        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: chartDatasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: !!title,
                        text: title,
                        color: '#e2e8f0',
                        font: { size: 14, weight: '600' }
                    },
                    legend: {
                        labels: { color: '#94a3b8' }
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        backgroundColor: 'rgba(15, 23, 42, 0.9)',
                        titleColor: '#e2e8f0',
                        bodyColor: '#cbd5e1',
                        borderColor: '#334155',
                        borderWidth: 1
                    }
                },
                scales: {
                    x: {
                        ticks: { color: '#64748b', maxTicksLimit: 10 },
                        grid: { color: 'rgba(51, 65, 85, 0.3)' }
                    },
                    y: {
                        ticks: { color: '#64748b' },
                        grid: { color: 'rgba(51, 65, 85, 0.3)' }
                    }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            }
        });
    },

    createBarChart: function (canvasId, labels, data, title, colors) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        const backgroundColors = colors || data.map(v =>
            v >= 0 ? 'rgba(34, 197, 94, 0.7)' : 'rgba(239, 68, 68, 0.7)'
        );

        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: title || 'Value',
                    data: data,
                    backgroundColor: backgroundColors,
                    borderColor: backgroundColors.map(c => c.replace('0.7', '1')),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: !!title,
                        text: title,
                        color: '#e2e8f0',
                        font: { size: 14, weight: '600' }
                    },
                    legend: { display: false }
                },
                scales: {
                    x: {
                        ticks: { color: '#64748b' },
                        grid: { color: 'rgba(51, 65, 85, 0.3)' }
                    },
                    y: {
                        ticks: { color: '#64748b' },
                        grid: { color: 'rgba(51, 65, 85, 0.3)' }
                    }
                }
            }
        });
    },

    createDoughnutChart: function (canvasId, labels, data, colors) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors || [
                        'rgba(34, 197, 94, 0.8)',
                        'rgba(239, 68, 68, 0.8)',
                        'rgba(234, 179, 8, 0.8)'
                    ],
                    borderColor: 'rgba(15, 23, 42, 0.8)',
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        labels: { color: '#94a3b8' }
                    }
                }
            }
        });
    },

    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    },

    createSparkline: function (canvasId, data, isPositive) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        const color = isPositive ? 'rgb(34, 197, 94)' : 'rgb(239, 68, 68)';
        const bgColor = isPositive ? 'rgba(34, 197, 94, 0.15)' : 'rgba(239, 68, 68, 0.15)';

        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.map((_, i) => i),
                datasets: [{
                    data: data,
                    borderColor: color,
                    backgroundColor: bgColor,
                    borderWidth: 1.5,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 0,
                    pointHoverRadius: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false }, tooltip: { enabled: false } },
                scales: {
                    x: { display: false },
                    y: { display: false }
                },
                animation: { duration: 400 },
                elements: { line: { borderJoinStyle: 'round' } }
            }
        });
    },

    destroyAllSparklines: function (prefix) {
        const keys = Object.keys(this.charts).filter(k => k.startsWith(prefix));
        keys.forEach(k => {
            this.charts[k].destroy();
            delete this.charts[k];
        });
    }
};

// Advanced Chart for Charts page
window.renderAdvancedChart = function (canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    // Destroy existing
    if (window.advancedChartInstance) {
        window.advancedChartInstance.destroy();
    }

    const { labels, prices, highs, lows, opens, chartType, indicators, symbol } = data;

    // Calculate indicators
    const datasets = [];

    // Main price dataset
    if (chartType === 'candlestick') {
        // For candlestick, we'll use OHLC bars simulation with line + fills
        datasets.push({
            label: symbol,
            data: prices,
            borderColor: '#00d09c',
            backgroundColor: 'rgba(0, 208, 156, 0.1)',
            borderWidth: 2,
            fill: true,
            tension: 0.1,
            pointRadius: 0,
            pointHoverRadius: 6
        });
        // High line
        datasets.push({
            label: 'High',
            data: highs,
            borderColor: 'rgba(34, 197, 94, 0.5)',
            borderWidth: 1,
            borderDash: [5, 5],
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
        // Low line
        datasets.push({
            label: 'Low',
            data: lows,
            borderColor: 'rgba(239, 68, 68, 0.5)',
            borderWidth: 1,
            borderDash: [5, 5],
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
    } else if (chartType === 'area') {
        datasets.push({
            label: symbol,
            data: prices,
            borderColor: '#00d09c',
            backgroundColor: 'rgba(0, 208, 156, 0.2)',
            borderWidth: 2,
            fill: true,
            tension: 0.4,
            pointRadius: 0
        });
    } else {
        datasets.push({
            label: symbol,
            data: prices,
            borderColor: '#00d09c',
            borderWidth: 2,
            fill: false,
            tension: 0.1,
            pointRadius: 0,
            pointHoverRadius: 6
        });
    }

    // Add indicators
    if (indicators.includes('sma20')) {
        const sma20 = calculateSMA(prices, 20);
        datasets.push({
            label: 'SMA 20',
            data: sma20,
            borderColor: '#fbbf24',
            borderWidth: 1.5,
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
    }

    if (indicators.includes('sma50')) {
        const sma50 = calculateSMA(prices, 50);
        datasets.push({
            label: 'SMA 50',
            data: sma50,
            borderColor: '#f97316',
            borderWidth: 1.5,
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
    }

    if (indicators.includes('ema12')) {
        const ema12 = calculateEMA(prices, 12);
        datasets.push({
            label: 'EMA 12',
            data: ema12,
            borderColor: '#8b5cf6',
            borderWidth: 1.5,
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
    }

    if (indicators.includes('bb')) {
        const bb = calculateBollingerBands(prices, 20);
        datasets.push({
            label: 'BB Upper',
            data: bb.upper,
            borderColor: 'rgba(59, 130, 246, 0.6)',
            borderWidth: 1,
            fill: false,
            tension: 0.1,
            pointRadius: 0
        });
        datasets.push({
            label: 'BB Lower',
            data: bb.lower,
            borderColor: 'rgba(59, 130, 246, 0.6)',
            borderWidth: 1,
            fill: '-1',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            tension: 0.1,
            pointRadius: 0
        });
    }

    window.advancedChartInstance = new Chart(ctx, {
        type: 'line',
        data: { labels, datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: { color: '#94a3b8', usePointStyle: true }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    backgroundColor: 'rgba(13, 17, 23, 0.95)',
                    titleColor: '#e2e8f0',
                    bodyColor: '#cbd5e1',
                    borderColor: '#30363d',
                    borderWidth: 1,
                    callbacks: {
                        label: function(ctx) {
                            return `${ctx.dataset.label}: $${ctx.parsed.y.toFixed(2)}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    ticks: { color: '#64748b', maxTicksLimit: 12 },
                    grid: { color: 'rgba(48, 54, 61, 0.5)' }
                },
                y: {
                    ticks: { 
                        color: '#64748b',
                        callback: function(value) { return '$' + value.toFixed(2); }
                    },
                    grid: { color: 'rgba(48, 54, 61, 0.5)' }
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });
};

// Technical Indicator Calculations
function calculateSMA(data, period) {
    const result = [];
    for (let i = 0; i < data.length; i++) {
        if (i < period - 1) {
            result.push(null);
        } else {
            const sum = data.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0);
            result.push(sum / period);
        }
    }
    return result;
}

function calculateEMA(data, period) {
    const result = [];
    const multiplier = 2 / (period + 1);
    let ema = data.slice(0, period).reduce((a, b) => a + b, 0) / period;
    
    for (let i = 0; i < data.length; i++) {
        if (i < period - 1) {
            result.push(null);
        } else if (i === period - 1) {
            result.push(ema);
        } else {
            ema = (data[i] - ema) * multiplier + ema;
            result.push(ema);
        }
    }
    return result;
}

function calculateBollingerBands(data, period) {
    const sma = calculateSMA(data, period);
    const upper = [];
    const lower = [];
    
    for (let i = 0; i < data.length; i++) {
        if (i < period - 1) {
            upper.push(null);
            lower.push(null);
        } else {
            const slice = data.slice(i - period + 1, i + 1);
            const mean = sma[i];
            const squaredDiffs = slice.map(v => Math.pow(v - mean, 2));
            const variance = squaredDiffs.reduce((a, b) => a + b, 0) / period;
            const stdDev = Math.sqrt(variance);
            upper.push(mean + 2 * stdDev);
            lower.push(mean - 2 * stdDev);
        }
    }
    return { upper, lower };
}
