# -*- coding: utf-8 -*-

import socket
import select

import joint_data_process

def main():
    #change here to specify the address of this server process
    host = '192.168.xxx.xxx'
    port = 13000
    backlog = 10
    bufsize = 4096

    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    readfds = set([server_sock])
    try:
        server_sock.bind((host, port))
        server_sock.listen(backlog)

        while True:
            rready, wready, xready = select.select(readfds, [], [])
            for sock in rready:
                if sock is server_sock:
                    conn, address = server_sock.accept()
                    print("new connection established")
                    readfds.add(conn)
                else:
                    msg = sock.recv(bufsize)
                    if len(msg) == 0:
                        sock.close()
                        print("disconnected")
                        readfds.remove(sock)
                    else:
                        #print(msg)
                        #sock.send(msg)
                        response = joint_data_process.process(msg)
                        sock.send(response)
    finally:
        for sock in readfds:
            sock.close()

    return

if __name__ == '__main__':
    main()
