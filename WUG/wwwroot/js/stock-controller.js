graph = null;
async function ConnectSignalR() {
    try {
        await connection.start();
        console.log("Connected to Exchange SignalR Hub");
        GetMessageHistory();
    } catch (err) {
        console.log("Failed to connect to SignalR> Retrying in 5 seconds...");
        console.log(err);
        setTimeout(() => ConnectSignalR(), 5000);
    }
}

function PrepSignalR() {
    // SIGNALR HOOKS //
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/ExchangeHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Retry connection if failed
    connection.onclose(async () => {
        await ConnectSignalR();
    });
    connection.on("StockTrade", (trade) => {

        var tradeObj = JSON.parse(trade);

        console.log(tradeObj)

        if (tradeObj.Ticker == ticker) {

        }
    })
}

function setgraph(_graph) {
    graph = _graph;
}

function updategraph2() {
    updategraph(999999)
}

function updategraph(price) {
    if (price != 999999) {
        chart.data.datasets[0].data.pop(0)
        chart.data.datasets[0].data.push(data['Price'])
        chart.update();

        prevprice = chart.data.datasets[0].data[chart.data.datasets[0].data.length - 1]
        var percent = Round(((chart.data.datasets[0].data[0] - prevprice) / prevprice) * 100.0);
        if (percent > 0) {
            document.getElementById("percText").style.color = "lightgreen";
            $('#percText').html("(+" + percent + "%)");
        }
        else if (percent < 0) {
            document.getElementById("percText").style.color = "lightcoral";
            $('#percText').html("(" + percent + "%)");
        }
        else {
            document.getElementById("percText").style.color = "white";
            $('#percText').html("(0%)");
        }
        return
    }
    if (graphsetting == "Minutes") {
        var url = `/api/securities/${ticker}/history?includetime=true&count=59`
    }
    if (graphsetting == "24h") {
        var url = `/api/securities/${ticker}/history?includetime=true&gethours=true&count=24`
    }
    if (graphsetting == "7d") {
        var url = `/api/securities/${ticker}/history?includetime=true&gethours=true&count=168`
    }
    GetHtml(url).then(history => {
        e = document.getElementById("priceChart")
        data = []
        labels = []
        prevprice = 0
        for (var i = 0; i < history.length; i++)
        {
            item = history[i]
            data.push(item[1])
            labels.push(`${item[0]}`)
            prevprice = item[1]
        }
        chart.data.datasets[0].data = data
        chart.data.labels = labels
        chart.update();

        var percent = Round(((history[0][1] - prevprice) / prevprice) * 100.0);

        if (percent > 0) {
            document.getElementById("percText").style.color = "lightgreen";
            $('#percText').html("(+" + percent + "%)");
        }
        else if (percent < 0) {
            document.getElementById("percText").style.color = "lightcoral";
            $('#percText').html("(" + percent + "%)");
        }
        else {
            document.getElementById("percText").style.color = "white";
            $('#percText').html("(0%)");
        }
    })
}

let myGreeting = setInterval(updategraph2, 59000);

function UpdateScreen(data) {
    updategraph(data['Price'])
    e = document.getElementById("openshares")
    e.innerHTML = `Available: ${data['SharesAvailable']} ${ticker}`
    e = document.getElementById("stockissued")
    e.innerHTML = `Stock Issued: ${data['TotalShares']}`
    e = document.getElementById("stockbalance")
    e.innerHTML = `Stock Balance: ¢${data['StockBalance']}`
    e = document.getElementById("stockprice")
    e.innerHTML = `$${data['Price']}`

    UpdateSellTotal(document.getElementById("amount-input-sell"))
    UpdateBuyTotal(document.getElementById("amount-input-buy"))
    if (data["TraderSVID"] != accountid) {
        return
    }

    var url = `/api/securities/${ticker}/ownership?accountid=${accountid}`
    GetHtml(url).then(data => {
        e = document.getElementById("owned-text")
        e.innerHTML = `You Own: ${data.toLocaleString(undefined, { maximumFractionDigits: 0} )} ${ticker}`
    })

    var url = `/api/entity/${accountid}/credits`
    GetHtml(url).then(data => {
        e = document.getElementById("balance-text")
        data = parseFloat(data)
        e.innerHTML = `Your Balance: $${data.toLocaleString(undefined, { maximumFractionDigits: 2 }) }`
    })
}

async function GetHtmlNonJson(url) {
    const response = await fetch(url);

    return await response.text();
}
async function GetHtml(url) {
    const response = await fetch(url);

    //console.log(await response.text());

    return await response.json();
}
function Submitted(element) {
    var text = element.contentDocument.body.innerHTML
    var e = document.getElementById("Output for buy/sell")
    e.innerHTML = text
}
function UpdateBuyTotal(element) {
    e = document.getElementById("buytotal")
    if (element.value == "" || element.value == null) {
        return
    }
    e.innerHTML = `Total: Calculating`
    var url = `/api/security/calcbuytotal?ticker=${ticker}&amount=${element.value}`
    GetHtml(url).then(data => {
        e = document.getElementById("buytotal")
        e.innerHTML = `Total: $${data}`
    })
}
function UpdateSellTotal(element) {
    e = document.getElementById("selltotal")
    if (element.value == "" || element.value == null) {
        return
    }
    e.innerHTML = `Total: Calculating`
    var url = `/api/security/calcselltotal?ticker=${ticker}&amount=${element.value}`
    GetHtml(url).then(data => {
        e = document.getElementById("selltotal")
        e.innerHTML = `Total: $${data}`
    })
}

function resetallbuttons() {
    e = document.getElementById("minutebutton")
    e.className = "btn btn-outline-primary"
    e = document.getElementById("hourbutton")
    e.className = "btn btn-outline-primary"
    e = document.getElementById("weekbutton")
    e.className = "btn btn-outline-primary"
}
function minutebutton(c) {
    graphsetting = "Minutes"
    resetallbuttons()
    e = document.getElementById("minutebutton")
    e.className = "btn btn-primary"
    updategraph(999999)
}
function hourbutton() {
    graphsetting = "24h"
    resetallbuttons()
    e = document.getElementById("hourbutton")
    e.className = "btn btn-primary"
    updategraph(999999)
}
function weekbutton() {
    graphsetting = "7d"
    resetallbuttons()
    e = document.getElementById("weekbutton")
    e.className = "btn btn-primary"
    updategraph(999999)
}