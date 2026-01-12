var dom = document.getElementById('container');
var myChart = echarts.init(dom, null, {
  renderer: 'svg',
  useDirtyRect: false
});
var app = {};

var option;

var valueGOOD = '$1';
var valueBAD = '$2';
var fontSize = '$3';
var percentPrecision = '$4';

if (isNaN(fontSize)) {
  fontSize = 24;
}
if (isNaN(percentPrecision)) {
  percentPrecision = 1;
}
if (isNaN(valueGOOD)) {
  valueGOOD = 6;
}
if (isNaN(valueBAD)) {
  valueBAD = 1;
}

option = {
  title: {
    text:
      ((valueGOOD / (valueGOOD + valueBAD)) * 100).toFixed(percentPrecision) +
      '%',
    textStyle: {
      color: '#000000',
      fontFamily: 'roboto condensed',
      fontSize: fontSize
    },
    left: 'center',
    top: 'center'
  },
  series: [
    {
      name: 'InnerChart',
      type: 'pie',
      clockwise: true,
      percentPrecision: percentPrecision,
      label: { show: false },
      silent: true,
      emphasis: { disabled: true },
      radius: ['79%', '89%'],
      data: [
        {
          value: valueGOOD,
          itemStyle: {
            color: '#6d91b6',
            borderColor: '#e4e4e4',
            borderWidth: 5
          }
        },
        {
          value: valueBAD,
          itemStyle: {
            color: '#e4e4e4',
            borderColor: '#e4e4e4',
            borderWidth: 5
          }
        }
      ]
    },
    {
      name: 'OuterChart',
      type: 'pie',
      clockwise: false,
      percentPrecision: percentPrecision,
      label: { show: false },
      silent: true,
      emphasis: { disabled: true },
      radius: ['94%', '99%'],
      data: [
        {
          value: valueBAD,
          itemStyle: {
            color: '#cc0036',
            borderColor: '#e4e4e4',
            borderWidth: 5
          }
        },
        {
          value: valueGOOD,
          itemStyle: {
            color: '#e4e4e4',
            borderColor: '#e4e4e4',
            borderWidth: 5
          }
        }
      ]
    }
  ]
};


if (option && typeof option === 'object') {
  myChart.setOption(option);
}

window.addEventListener('resize', myChart.resize);