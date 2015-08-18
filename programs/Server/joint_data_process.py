# -*- coding: utf-8 -*-

import struct

#関節名一覧
#NOTE: ココの並び順はC#, Choregrapheのと合ってないとダメ
_jointnames = [
    "LShoulderPitch",
    "LShoulderRoll",
    "LElbowYaw",
    "LElbowRoll",
    "LWristYaw",
    "RShoulderPitch",
    "RShoulderRoll",
    "RElbowYaw",
    "RElbowRoll",
    "RWristYaw",
    "HipPitch",
    "LHand",
    "RHand"
]

#ここでは基本的にバイナリとしてしか持たない
#…というかデータの中すら見ないイメージ
_angles = '\x00' * len(_jointnames) * 4

#送られるデータの形式と返信の内容はココで規定される
def process(msg):
    global _angles

    if len(msg) < 4: return msg
    header = msg[:4]

    #関節名の取得リクエストへの応答
    if header == "getn":
        return " ".join(_jointnames)

    #関節の値を取得する
    if header == "getj":
        return _angles

    #関節の値をセットする
    if header == "setj":
        data = msg[4:]
        #バイト長が合ってない怪しいデータは無視
        expected_size = len(_jointnames) * 4
        if len(data) == expected_size:
            _angles = data
            return "succeed"
        else:
            return "failed"


