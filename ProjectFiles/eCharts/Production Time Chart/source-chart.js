var dom = document.getElementById('container');
var myChart = echarts.init(dom, null, {
	renderer: 'svg',
	useDirtyRect: false
});
var app = {};

var option;

option = {
	series: [{
		name: 'Chart',
		type: 'pie',
		z: 1,
		percentPrecision: 0,
		clockwise: true,
		startAngle: 90,
		endAngle: 450,
		minAngle: 10,
		padAngle: 0,
		stillShowZeroSum: false,
		silent: true,
		showEmptyCircle: true,
		emptyCircleStyle: { color: '$bg$' },
		label: { show: false },
		radius: ['84%', '99%'],
		data: [
			{$0$},
			{$1$},
			{$2$},
			{$3$},
			{$4$},
			{$5$},
			{$6$},
			{$7$},
			{$8$},
			{$9$}
		]
	}]
};


if (option && typeof option === 'object') {
	myChart.setOption(option);
}

window.addEventListener('resize', myChart.resize);