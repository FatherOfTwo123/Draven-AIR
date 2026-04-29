const fs = require('fs')
const net = require('net')
const path = require('path')
const { spawn } = require('child_process')

const repoRoot = __dirname
const gameRoot = process.argv[2]
  ? path.resolve(process.argv[2])
  : process.env.DRAVEN_GAME_CLIENT_ROOT
    ? path.resolve(process.env.DRAVEN_GAME_CLIENT_ROOT)
    : ''
const deployDir = path.join(gameRoot, 'RADS', 'projects', 'lol_air_client', 'releases', '0.0.1.88', 'deploy')
const logPath = path.join(repoRoot, 'direct_air_maestro.log')
const clientPort = 8393
const gamePort = 8394

if (!gameRoot || !fs.existsSync(path.join(deployDir, 'LolClient.exe'))) {
  throw new Error('4.20 game client not found. Pass the client root folder to direct_air_maestro.js.')
}

const MSG = {
  GAMECLIENT_CREATE: 0,
  GAMECLIENT_STOPPED: 1,
  GAMECLIENT_CRASHED: 2,
  CLOSE: 3,
  HEARTBEAT: 4,
  REPLY: 5,
  LAUNCHERCLIENT: 6,
  GAMECLIENT_ABANDONED: 7,
  GAMECLIENT_LAUNCHED: 8,
  GAMECLIENT_CONNECTED_TO_SERVER: 10,
  CHATMESSAGE_TO_GAME: 11,
  CHATMESSAGE_FROM_GAME: 12,
}

let gameSocket = null

function log(...parts) {
  const line = `[${new Date().toISOString()}] ${parts.join(' ')}`
  fs.appendFileSync(logPath, line + '\n')
}

function packet(command, body = Buffer.alloc(0)) {
  const header = Buffer.alloc(16)
  header.writeUInt32LE(0x10, 0)
  header.writeUInt32LE(0x01, 4)
  header.writeUInt32LE(command >>> 0, 8)
  header.writeUInt32LE(body.length >>> 0, 12)
  return Buffer.concat([header, body])
}

function attachSocket(name, socket, onMessage) {
  log(name, 'connected', socket.remoteAddress || '', socket.remotePort || '')
  let backlog = Buffer.alloc(0)

  const sendHeartbeat = () => {
    try {
      socket.write(packet(MSG.HEARTBEAT))
      log(name, 'send', 'HEARTBEAT')
    } catch {}
  }

  sendHeartbeat()
  const heartbeat = setInterval(sendHeartbeat, 25000)

  socket.on('data', (chunk) => {
    backlog = Buffer.concat([backlog, chunk])
    while (backlog.length >= 16) {
      const head0 = backlog.readUInt32LE(0)
      const head1 = backlog.readUInt32LE(4)
      const command = backlog.readUInt32LE(8)
      const bodyLen = backlog.readUInt32LE(12)
      if (backlog.length < 16 + bodyLen) break
      const body = backlog.slice(16, 16 + bodyLen)
      backlog = backlog.slice(16 + bodyLen)
      const bodyText = body.toString('utf8').replace(/\0/g, '')
      log(name, 'recv', `head0=${head0}`, `head1=${head1}`, `cmd=${command}`, `bodyLen=${bodyLen}`, bodyText)
      onMessage(command, body, bodyText, socket)
    }
  })

  socket.on('close', () => {
    clearInterval(heartbeat)
    log(name, 'disconnected')
  })

  socket.on('error', (err) => {
    clearInterval(heartbeat)
    log(name, 'socket error', err.message)
  })
}

function startClientServer() {
  const server = net.createServer((socket) => {
    attachSocket('client', socket, (command, body, bodyText, clientSocket) => {
      switch (command) {
        case MSG.GAMECLIENT_CREATE:
          clientSocket.write(packet(MSG.REPLY))
          break
        case MSG.HEARTBEAT:
          clientSocket.write(packet(MSG.REPLY))
          break
        case MSG.CHATMESSAGE_TO_GAME:
          if (gameSocket && !gameSocket.destroyed) {
            gameSocket.write(packet(MSG.CHATMESSAGE_TO_GAME, body))
          }
          break
        case MSG.CLOSE:
          log('client', 'requested close')
          clientSocket.write(packet(MSG.REPLY))
          break
        default:
          log('client', 'ignored command', String(command), bodyText)
          break
      }
    })
  })

  server.on('error', (err) => log('server error', err.message))
  server.listen(clientPort, '127.0.0.1', () => log('maestro server listening', clientPort))
  return server
}

function startGameServer() {
  const server = net.createServer((socket) => {
    gameSocket = socket
    attachSocket('game', socket, (command, body, bodyText, gameConn) => {
      switch (command) {
        case MSG.HEARTBEAT:
          gameConn.write(packet(MSG.REPLY))
          break
        case MSG.GAMECLIENT_STOPPED:
        case MSG.GAMECLIENT_CRASHED:
        case MSG.GAMECLIENT_ABANDONED:
        case MSG.GAMECLIENT_LAUNCHED:
        case MSG.GAMECLIENT_CONNECTED_TO_SERVER:
        case MSG.CHATMESSAGE_FROM_GAME:
          break
        case MSG.CLOSE:
          log('game', 'requested close')
          break
        default:
          log('game', 'ignored command', String(command), bodyText)
          break
      }
    })
    socket.on('close', () => {
      if (gameSocket === socket) gameSocket = null
    })
  })
  server.on('error', (err) => log('game server error', err.message))
  server.listen(gamePort, '127.0.0.1', () => log('game server listening', gamePort))
  return server
}

function startClient() {
  const exe = path.join(deployDir, 'LolClient.exe')
  const args = [
    '-runtime', '.\\',
    '-nodebug',
    'META-INF\\AIR\\application.xml',
    '.\\',
    '--',
    String(clientPort),
    '--host=127.0.0.1',
    '--xmpp_server_url=127.0.0.1',
    '--lq_uri=http://127.0.0.1:8080',
    '--getClientIpURL=http://127.0.0.1:8080',
  ]
  log('launching client', exe, args.join(' '))
  const child = spawn(exe, args, {
    cwd: deployDir,
    detached: true,
    stdio: 'ignore',
    env: { ...process.env, __COMPAT_LAYER: 'ElevateCreateProcess' },
  })
  child.unref()
}

fs.writeFileSync(logPath, '')
startClientServer()
startGameServer()
setTimeout(startClient, 1000)
