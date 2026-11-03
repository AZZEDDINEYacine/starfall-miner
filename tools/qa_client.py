#!/usr/bin/env python3
"""One JSON request per localhost connection to the opt-in C# QA scene.

Keep this helper's name distinct from Python's standard-library inspect module.
"""
import json
import socket
import sys


def request(command="state", **fields):
    with socket.create_connection(("127.0.0.1", 47831), timeout=10) as connection:
        connection.sendall((json.dumps(dict(command=command, **fields)) + "\n").encode())
        response = connection.makefile("r", encoding="utf-8").readline()
        if not response:
            raise RuntimeError("Inspection disconnected without a response")
        value = json.loads(response)
        if "error" in value:
            raise RuntimeError(value["error"])
        return value


if __name__ == "__main__":
    payload = json.loads(sys.argv[1]) if len(sys.argv) > 1 else {"command": "state"}
    print(json.dumps(request(**payload), indent=2))
