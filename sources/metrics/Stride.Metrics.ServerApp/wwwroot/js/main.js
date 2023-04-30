const plotData = [ 
  {id:'installs-trend', getData: getData_InstallTrend, getLabel: getLabel_InstallTrend},
  {id:'installs-per-month', getData: getCount, getLabel: getMonthYearLabel},
  {id:'installs-last-days', getData: getCount, getLabel: getLabel_InstallsLastDays},
  {id:'quitting-count', getData: getCount, getLabel: getMonthYearLabel},
  {id:'retain-percentage', route:'quitting-count', getData: getRetainPercentage, getLabel: getMonthYearLabel},
  {id:'stride-downloads', getData: getCount, getLabel: getMonthYearLabel},
  {id:'vsx-downloads', getData: getCount, getLabel: getMonthYearLabel},
  {id:'total-users-per-country',route: 'get-countries/0', getData: getData_Countries, getLabel: getLabel_Countries},
  {id:'users-per-country',route: 'get-countries/31', getData: getData_Countries, getLabel: getLabel_Countries},
  {id:'total-users-per-month',route: 'active-users-per-month/0', getData: getCount, getLabel: getMonthYearLabel},
  {id:'total-users-per-day',route: 'active-users-per-day/0', getData: getCount, getLabel: getDayMonthYearLabel},
  {id:'active-users-per-month',route: 'active-users-per-month/5', getData: getCount, getLabel: getMonthYearLabel},
  {id:'active-users', getData: getData_ActiveUsers, getLabel: getLabel_ActiveUsers},
  {id:'high-usage', getData: getData_HighUsage, getLabel: getLabel_HighUsage},
  {id:'projects-users', getData: getData_ProjectUsers, getLabel: getLabel_ProjectUsers},
  {id:'usage-per-version', getData: getData_UsagePerVersion, getLabel: getLabel_UsagePerVersion},
  {id:'crashes-per-version', getData: getData_CrashesPerVersion, getLabel: getLabel_CrashesPerVersion},
  {id:'platforms-usage', getData: getCount, getLabel: getLabel_PlatformsUsage},
];

plotData.forEach(pd =>
  initChart(pd.id, pd.getLabel, pd.getData, pd.route ?? pd.id )
);

function getData_Countries(response) {
  return null;
}
function getData_ActiveUsersPerMonth(response) {
  return null;
}
function getData_ActiveUsers(response) {
  return null;
}
function getData_HighUsage(response) {
  return null;
}
function getData_ProjectUsers(response) {
  return null;
}
function getData_UsagePerVersion(response) {
  return null;
}
function getData_CrashesPerVersion(response) {
  return null;
}
function getDayMonthYearLabel(response){
    return response.flatMap(({ day, month, year }) => new Date(year,month,day).toLocaleDateString("en-US", { month: 'short', day: 'numeric' } ) );
}
function getLabel_QuittingCount(response) {
  return null;
}
function getLabel_RetainPercentage(response) {
  return null;
}
function getLabel_Countries(response) {
  return null;
}
function getLabel_ActiveUsers(response) {
  return null;
}
function getLabel_HighUsage(response) {
  return null;
}
function getLabel_ProjectUsers(response) {
  return null;
}
function getLabel_UsagePerVersion(response) {
  return null;
}
function getLabel_CrashesPerVersion(response) {
  return null;
}
function getRetainPercentage(response){
    const counts = getCount(response);
    return counts.map((x,i) => i > 0 ? (1 - (x/counts[i-1]))*100 : 0);
}

function getCount(response) {
  return response.flatMap(({ count }) => count );
}

function getData_InstallTrend(response) {
  return response.flatMap(({ year, ...months }) => Object.entries(months).flatMap(([month, value]) => {
    if (value === 0 || month === "count") {
      return []; // skip this month
    }
    return value;
  })
  );
}

function getMonthYearLabel(response) {
  return response.flatMap(({ year, month }) => 
  `${new Date(year,month,0).toLocaleDateString("en-US",{ month: 'long'})} ${year}`
  );
}
function getLabel_InstallsLastDays(response) {
  return response.flatMap(({ date }) => 
  `${ new Date(date).toLocaleDateString("en-US", { month: 'short', day: 'numeric' } ) }`
  );
}
function getLabel_PlatformsUsage(response) {
  return response.flatMap(({ platform }) => platform );
}
function getLabel_InstallTrend(response) {
  return response.flatMap(({ year, ...months }) => Object.entries(months).flatMap(([month, value]) => {
    if (value === 0 || month === "count") {
      return []; // skip this month
    }
    month = month.charAt(0).toUpperCase() + month.slice(1);
    return `${month} ${year}`;
  })
  );
}

function initChart(actionName, getLabelStrategy, getDataStrategy, endpoint) {
  const canvas = $(`#${actionName}`)[0];

  let labels = [];
  let data = [];

  fetch(`api/${endpoint}`).
    then(response => response.json()).
    then(result => {
      labels = getLabelStrategy(result);

      data = getDataStrategy(result);

      createChart(canvas, labels, data);
    }).
    catch(error => console.log(error));
}

function createChart(chartCanvas, labels, data) {

  new Chart(chartCanvas, {
    type: chartCanvas.dataset.charttype,
    data: {
      labels: labels,
      datasets: [{
        label: chartCanvas.dataset.labelname,
        data: data,
        borderWidth: 1,
        fill: true
      }]
    },
    options: {
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  });
} 
