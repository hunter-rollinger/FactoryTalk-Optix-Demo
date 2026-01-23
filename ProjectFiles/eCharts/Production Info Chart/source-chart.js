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
		minAngle: 5,
		padAngle: 0,
		stillShowZeroSum: true,
		silent: true,
		showEmptyCircle: true,
		emptyCircleStyle: { color: '$3$' },
		label: { show: false },
		emphasis: { disabled: true },
		select: { disabled: true },
		radius: ['79%', '89%'],
		data: [{
			value: $1$,
			itemStyle: {
				color: '$2$',
				borderColor: '$3$',
				borderWidth: 5
			}
		},
		{
			value: $4$,
			itemStyle: {
				color: '$3$',
				borderColor: '$3$',
				borderWidth: 5
			}
		}]
	},
	{
		name: 'OuterChart',
		type: 'pie',
		clockwise: false,
		percentPrecision: 0,
		selectedMode: false,
		startAngle: 90,
		endAngle: 450,
		minAngle: 5,
		padAngle: 0,
		stillShowZeroSum: true,
		silent: true,
		showEmptyCircle: true,
		emptyCircleStyle: { color: '$6$' },
		label: { show: false },
		emphasis: { disabled: true },
		select: { disabled: true },
		radius: ['94%', '99%'],
		data: [{
			value: $4$,
			itemStyle: {
				color: '$5$',
				borderColor: '$6$',
				borderWidth: 5
			}
		},
		{
			value: $1$,
			itemStyle: {
				color: '$6$',
				borderColor: '$6$',
				borderWidth: 5
			}
		}]
	}]
};


if (option && typeof option === 'object') {
	myChart.setOption(option);
}

window.addEventListener('resize', myChart.resize);