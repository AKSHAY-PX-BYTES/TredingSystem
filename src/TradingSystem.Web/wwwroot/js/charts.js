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
