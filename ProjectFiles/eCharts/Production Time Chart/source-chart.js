var dom = document.getElementById('container');
var myChart = echarts.init(dom, null, {
	renderer: 'svg',
	useDirtyRect: false
});
var app = {};

var option;

option = {
	series: [{
		name: 'InnerChart',
		type: 'pie',
		percentPrecision: 0,
		selectedMode: false,
		clockwise: true,
		startAngle: 90,
		endAngle: 450,
		minAngle: 0,
		padAngle: 0,
		stillShowZeroSum: false,
		silent: true,
		showEmptyCircle: true,
		emptyCircleStyle: { color: '$0' },
		label: { show: false },
		emphasis: { disabled: true },
		select: { disabled: true },
		radius: ['89%', '99%'],
		data: [
			{ value: $1, name: '', itemStyle: { color: '$11', borderColor: '$0', borderWidth: 3 } },
			{ value: $2, name: '', itemStyle: { color: '$12', borderColor: '$0', borderWidth: 3 } },
			{ value: $3, name: '', itemStyle: { color: '$13', borderColor: '$0', borderWidth: 3 } },
			{ value: $4, name: '', itemStyle: { color: '$14', borderColor: '$0', borderWidth: 3 } },
			{ value: $5, name: '', itemStyle: { color: '$15', borderColor: '$0', borderWidth: 3 } },
			{ value: $6, name: '', itemStyle: { color: '$16', borderColor: '$0', borderWidth: 3 } },
			{ value: $7, name: '', itemStyle: { color: '$17', borderColor: '$0', borderWidth: 3 } },
			{ value: $8, name: '', itemStyle: { color: '$18', borderColor: '$0', borderWidth: 3 } },
			{ value: $9, name: '', itemStyle: { color: '$19', borderColor: '$0', borderWidth: 3 } },
			{ value: $10, name: '', itemStyle: { color: '$20', borderColor: '$0', borderWidth: 3 } }
		]
	}]
};


if (option && typeof option === 'object') {
	myChart.setOption(option);
}

window.addEventListener('resize', myChart.resize);