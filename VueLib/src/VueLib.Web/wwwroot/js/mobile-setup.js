/* ============================================================
 * VueLib 移动端业务组件 - Setup 逻辑
 * 避免 Razor section 中复杂 JS 导致的解析错误
 * ============================================================ */
(function () {
    'use strict';

    // ===== NDataTable =====
    window.__nutDataTableSetup = function (props, comInnerInfo, context) {
        const { ref, computed, onMounted } = Vue;
        const columns = props.jsonconfig.options.comoptions.columns || [];
        const pageSize = props.jsonconfig.options.comoptions.pageSize || 20;
        comInnerInfo.items = ref([]);
        comInnerInfo.total = ref(0);
        comInnerInfo.page = ref(1);
        comInnerInfo.loading = ref(false);

        const totalPages = computed(function () {
            return Math.ceil(comInnerInfo.total.value / pageSize) || 1;
        });

        function fmtVal(col, val) {
            if (val == null) return '';
            if (col.type === 'money') return '$' + Number(val).toFixed(2);
            if (col.type === 'number') return Number(val).toLocaleString();
            if (col.type === 'percent') return Number(val).toFixed(1) + '%';
            if (col.type === 'date') return String(val).split('T')[0];
            return String(val);
        }

        function loadData() {
            comInnerInfo.loading.value = true;
            const apiUrl = props.jsonconfig.options.comoptions.apiUrl;
            const reportType = props.jsonconfig.options.comoptions.reportType;
            const stationId = props.jsonconfig.options.comoptions.stationId || 0;
            const startDate = props.jsonconfig.options.comoptions.startDate || '';
            const endDate = props.jsonconfig.options.comoptions.endDate || '';

            if (!apiUrl) {
                comInnerInfo.items.value = props.jsonconfig.options.comoptions.staticData || [];
                comInnerInfo.total.value = comInnerInfo.items.value.length;
                comInnerInfo.loading.value = false;
                return;
            }

            var url = apiUrl + '?reportType=' + reportType + '&stationId=' + stationId +
                '&startDate=' + startDate + '&endDate=' + endDate +
                '&page=' + comInnerInfo.page.value + '&pageSize=' + pageSize;

            fetch(url, { credentials: 'include' })
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    comInnerInfo.items.value = d.items || d.Items || [];
                    comInnerInfo.total.value = d.total || d.Total || 0;
                })
                .catch(function () {
                    comInnerInfo.items.value = [];
                    comInnerInfo.total.value = 0;
                })
                .finally(function () {
                    comInnerInfo.loading.value = false;
                });
        }

        function prev() {
            if (comInnerInfo.page.value > 1) {
                comInnerInfo.page.value--;
                loadData();
            }
        }

        function next() {
            if (comInnerInfo.page.value * pageSize < comInnerInfo.total.value) {
                comInnerInfo.page.value++;
                loadData();
            }
        }

        onMounted(function () {
            if (props.jsonconfig.options.comoptions.autoLoad !== false) {
                loadData();
            }
        });

        // 暴露到模板
        comInnerInfo.columns = columns;
        comInnerInfo.totalPages = totalPages;
        comInnerInfo.fmtVal = fmtVal;
        comInnerInfo.loadData = loadData;
        comInnerInfo.prev = prev;
        comInnerInfo.next = next;

        return null;
    };

    // ===== NEChart =====
    window.__nutEChartSetup = function (props, comInnerInfo, context) {
        const { ref, onMounted, onUnmounted, nextTick } = Vue;
        const chartRef = ref(null);
        var chartInstance = null;
        comInnerInfo.loading = false;
        comInnerInfo.hasData = false;

        function initChart() {
            if (!chartRef.value || !window.echarts) return;
            if (chartInstance) chartInstance.dispose();
            chartInstance = window.echarts.init(chartRef.value);
        }

        function setOption(option) {
            if (chartInstance) {
                chartInstance.setOption(option, true);
                comInnerInfo.hasData = true;
            }
        }

        function loadChart() {
            comInnerInfo.loading = true;
            var apiUrl = props.jsonconfig.options.comoptions.apiUrl;
            var chartField = props.jsonconfig.options.comoptions.chartField;
            var valueField = props.jsonconfig.options.comoptions.valueField;
            var chartType = props.jsonconfig.options.comoptions.chartType || 'bar';
            var color = props.jsonconfig.options.comoptions.color || '#4A90D9';

            if (!apiUrl) {
                var staticData = props.jsonconfig.options.comoptions.staticData || [];
                renderChart(staticData, chartField, valueField, chartType, color);
                comInnerInfo.loading = false;
                return;
            }

            fetch(apiUrl, { credentials: 'include' })
                .then(function (r) { return r.json(); })
                .then(function (list) {
                    renderChart(list || [], chartField, valueField, chartType, color);
                })
                .catch(function () { comInnerInfo.hasData = false; })
                .finally(function () { comInnerInfo.loading = false; });
        }

        function renderChart(list, chartField, valueField, chartType, color) {
            var categories = [];
            var values = [];
            if (Array.isArray(list) && list.length) {
                list.forEach(function (item) {
                    categories.push(String(item[chartField] || ''));
                    values.push(Number(item[valueField]) || 0);
                });
            }
            comInnerInfo.hasData = categories.length > 0;
            if (!comInnerInfo.hasData || !chartInstance) return;

            var option = {
                tooltip: { trigger: 'axis', formatter: function (p) { return p[0].name + '<br/>$' + Number(p[0].value).toFixed(2); } },
                grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
                xAxis: { type: 'category', data: categories, axisLabel: { rotate: categories.length > 8 ? 30 : 0, fontSize: 10 } },
                yAxis: { type: 'value', axisLabel: { formatter: '${value}' } },
                series: [{ type: chartType, data: values, itemStyle: { color: color, borderRadius: [4, 4, 0, 0] } }]
            };
            chartInstance.setOption(option, true);
        }

        onMounted(function () {
            nextTick(function () {
                initChart();
                if (props.jsonconfig.options.comoptions.autoLoad !== false) {
                    loadChart();
                }
            });
        });

        onUnmounted(function () {
            if (chartInstance) chartInstance.dispose();
        });

        comInnerInfo.chartRef = chartRef;
        comInnerInfo.loadChart = loadChart;
        comInnerInfo.setOption = setOption;
        comInnerInfo.initChart = initChart;

        return null;
    };

    // ===== NReportFilter =====
    window.__nutReportFilterSetup = function (props, comInnerInfo, context) {
        const { ref, computed, onMounted } = Vue;
        comInnerInfo.stations = ref([]);
        comInnerInfo.localStationId = ref(props.jsonconfig.options.comoptions.stationId || 0);
        comInnerInfo.localTopN = ref(props.jsonconfig.options.comoptions.topN || 10);
        comInnerInfo.showStationPicker = ref(false);
        comInnerInfo.showTopPicker = ref(false);
        comInnerInfo.showDatePicker = ref(false);
        comInnerInfo.startDate = ref(props.jsonconfig.options.comoptions.startDate || '');
        comInnerInfo.endDate = ref(props.jsonconfig.options.comoptions.endDate || '');

        comInnerInfo.topOptions = [
            { text: 'Top 5', value: 5 },
            { text: 'Top 10', value: 10 },
            { text: 'Top 20', value: 20 },
            { text: 'All', value: 0 }
        ];

        comInnerInfo.stationColumns = computed(function () {
            return comInnerInfo.stations.value.map(function (s) {
                return { text: s.Name || s.name, value: s.Id || s.id };
            });
        });

        comInnerInfo.selectedStationName = computed(function () {
            var found = comInnerInfo.stations.value.find(function (s) { return (s.Id || s.id) === comInnerInfo.localStationId.value; });
            return found ? (found.Name || found.name) : 'All Stations';
        });

        comInnerInfo.topText = computed(function () {
            var found = comInnerInfo.topOptions.find(function (o) { return o.value === comInnerInfo.localTopN.value; });
            return found ? found.text : 'Top 10';
        });

        comInnerInfo.dateRangeText = computed(function () {
            if (comInnerInfo.startDate.value && comInnerInfo.endDate.value) {
                return comInnerInfo.startDate.value + ' ~ ' + comInnerInfo.endDate.value;
            }
            return 'Last 7 days';
        });

        comInnerInfo.defaultDates = computed(function () {
            function fmt(d) {
                return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
            }
            var start = comInnerInfo.startDate.value || fmt(new Date(Date.now() - 7 * 86400000));
            var end = comInnerInfo.endDate.value || fmt(new Date());
            return [start, end];
        });

        comInnerInfo.onStationConfirm = function (p) {
            if (p.selectedOptions && p.selectedOptions[0]) {
                comInnerInfo.localStationId.value = p.selectedOptions[0].value;
            }
            comInnerInfo.showStationPicker.value = false;
        };

        comInnerInfo.onTopConfirm = function (p) {
            if (p.selectedOptions && p.selectedOptions[0]) {
                comInnerInfo.localTopN.value = p.selectedOptions[0].value;
            }
            comInnerInfo.showTopPicker.value = false;
        };

        comInnerInfo.onDateConfirm = function (values) {
            if (!values || !values.length) return;
            var start = Array.isArray(values[0]) ? values[0][3] : values[0];
            var end = values[1] ? (Array.isArray(values[1]) ? values[1][3] : values[1]) : start;
            comInnerInfo.startDate.value = start;
            comInnerInfo.endDate.value = end;
            comInnerInfo.showDatePicker.value = false;
        };

        comInnerInfo.onSearch = function () {
            // 触发搜索事件，可通过 comlisteners 扩展
        };

        onMounted(function () {
            if (props.jsonconfig.options.comoptions.stations) {
                comInnerInfo.stations.value = props.jsonconfig.options.comoptions.stations;
            }
        });

        return null;
    };

    // ===== NLoginCard =====
    window.__nutLoginCardSetup = function (props, comInnerInfo, context) {
        const { ref } = Vue;
        comInnerInfo.username = ref('');
        comInnerInfo.password = ref('');
        comInnerInfo.rememberMe = ref(true);
        comInnerInfo.showPassword = ref(false);
        comInnerInfo.loading = ref(false);

        comInnerInfo.onLogin = function () {
            if (!comInnerInfo.username.value.trim()) {
                if (window.nutui && window.nutui.showToast) {
                    window.nutui.showToast.text('Please enter username');
                }
                return;
            }
            if (!comInnerInfo.password.value) {
                if (window.nutui && window.nutui.showToast) {
                    window.nutui.showToast.text('Please enter password');
                }
                return;
            }
            comInnerInfo.loading.value = true;
            setTimeout(function () { comInnerInfo.loading.value = false; }, 1000);
        };

        return null;
    };

    console.log('[mobile-setup] 移动端组件 setup 函数已加载');
})();
