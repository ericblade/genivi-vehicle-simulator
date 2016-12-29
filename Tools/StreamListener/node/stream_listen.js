const net = require('net');

const server = net.createServer((c) => {
    console.log('**** connected!!');
    c.on('data', (data) => {
        console.log(data.toString('utf8'));
    });
});

server.listen(9000, () => {
    console.log('**** socket server bound');
});
