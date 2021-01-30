# Crytometheus

Cryptometheus is a dotnet-core-powered [Prometheus](https://prometheus.io/) exporter for Crypto Currencies.  Crypto prices come from the [Cryptonator](https://www.cryptonator.com/) [API](https://www.cryptonator.com/api).

# Downloads

Soon!

# Configuration

There are two arguments that can be passed into Cryptometheus.  The tickers you want to watch, and the rate limit.  They can be supplied via the command line or via environment variables.  If a command line argument is specified, the values contained with the variables are ignored.

**tickers**

The "tickers" argument can be supplied on the command line or via environment variable.  It contains a ';' separated list of tickers you want to watch.  At least one ticker must be specified.

Sample usage: 

```
Cryptometheus.exe --tickers=btc-usd;doge-usd;algo-usd;xlm-usd;
```

**rate_limit**

The "rate_limit" argument specifies often to to wait between _each_ API query in seconds.  This application is a guest of a free API, so we don't want to flood it.  This is defaulted to 10 seconds.

How rate_limit works is the passed in tickers are queried in a round-robin style every time the rate limit is exceeded.  So if the passed in command line arguments are:

```
Cryptometheus.exe --tickers=btc-usd;doge-usd;algo-usd;xlm-usd; --rate_limit=10
```

Cryptometheus will first query btc-usd, wait 10 seconds, query doge-usd, wait 10 seconds, query algo-usd, wait 10 seconds, query xlm-usd, wait 10 seconds, and then start over to btc-usd.

Cryptonator only updates prices every 30 seconds.  So, if you are only querying 1 crypto, you may want to consider increasing the rate_limit value to at least 30 seconds.  If you are querying many cryptos, you may want to decrease it, but not so much you flood their server.

# Prometheus Configuration

When Cryptometheus is running, it will serve metrics on ```http://localhost:port/metrics```. All other URLs result in a blank page.  You will have to have your prometheus configuration point to this URL.

On Prometheus, the prices of tickers are guages, whose name is the same values passed into the ```tickers``` command line argument or environment variable, but '-' is replaced with '_' since Prometheus does not support '-'.  They are also in all caps.
