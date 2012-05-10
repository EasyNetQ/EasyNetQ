// LongRunningServer.js
// This is a little node server that returns after an interval you define in the query string. 
// You'll need node.js for windows (http://nodejs.org)
// Run like this:
//      node LongRunningServer.js
//
// Try it out with curl:
//      $ curl http://localhost:1338/?timeout=999
//      I waited for 999 milliseconds

var http = require('http');
var url = require('url');

http.createServer(function (req, res) {
    var parsedUrl = url.parse(req.url, true);

    if (parsedUrl.pathname === '/') {
        var timeout = parseInt(parsedUrl.query['timeout']);
        timeout = isNaN(timeout) ? 0 : timeout;
        setTimeout(function () {
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end('I waited for ' + timeout + ' milliseconds\n');
        }, timeout);
    } else {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('Not Found Here\n');
    }

}).listen(1338);

console.log('LongRunningServer is at http://localhost:1338/');