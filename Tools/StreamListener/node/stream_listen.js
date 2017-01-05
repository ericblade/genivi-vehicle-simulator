const net = require('net');
let connections = [];
let timerInterval = null;
let currentData = {};
let lastSentData = {};

const sendChangedData = () => {
    let outStr = '';
    Object.keys(currentData).forEach((k) => {
        if (lastSentData[k] !== currentData[k]) {
            outStr += `${k}, ${currentData[k]}\r\n`;
            lastSentData[k] = currentData[k];
        }
    });
    connections.forEach((c) => {
        if (!c.destroyed)
            c.write(outStr);
    });
};

// data from genivi-simulator is in form
// FieldName, data, number.
// Last number appears to be a timestamp in number of seconds since start, probably for data
// gathering purposes.
const parseIncomingData = (data) => {
    const arrayOfLines = data.match(/[^\r\n]+/g);
    arrayOfLines.forEach((line) => {
        const [fieldName, fieldData, fieldTimeStamp] = line.split(',');
        if (currentData[fieldName] !== fieldData) { // only console log changes in data
            console.log(`${fieldName}, ${fieldData}, ${fieldTimeStamp}`);
        }
        currentData[fieldName] = fieldData;
    });
};

const lostConnection = (c) => {
    connections = connections.filter((connection) => connection !== c);
    console.log(`**** lost connection numConnections=${connections.length}`);
    if (connections.length === 0) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

const server = net.createServer((c) => {
    connections.push(c);
    console.log(`**** connected!! numConnections=${connections.length}`);
    if (!timerInterval) {
        timerInterval = setInterval(sendChangedData, 20);
    }
    c.on('data', (data) => {
        parseIncomingData(data.toString('utf8'));
    });
    c.on('close', () => lostConnection(c));
    c.on('error', () => {}); // dummy handler, so that socket receive errors don't crash. Assume fatals will destroy the socket. close should occur immediately after.
});

server.listen(9000, () => {
    console.log('**** socket server bound');
});
