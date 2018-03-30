# FlyTek_Unity<br>
科大讯飞SDK+Unity，支持Android版本和PC版本（实现了TTS,ISR,ISE）；<br>
注意事项：<br>
1.so和dll使用的是自己讯飞开发平台的appid；如果使用自己的appid，需要替换Plugins文件夹下对应的dll和so；<br>
2.TTS里sessionParams设置的voice_name为john，所以只支持英文。如果需要支持中文，改为xiaoyan等中文发音；<br>
3.ISE在PC平台，如果评测的是中文，会有乱码问题（目前还没有解决方案）；Android平台没有问题。英文的话，PC和Android平台都没有问题。<br>
