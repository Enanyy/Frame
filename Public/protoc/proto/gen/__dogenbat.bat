@echo off

echo ^@echo off
echo.
echo cd ..\
echo protobuf-net\ProtoGen\protogen.exe ^^
cd ..\
for %%i in (*.txt) do echo -i:%%i ^^
cd gen
echo -o:PBMessage\PBMessage.cs -ns:PBMessage
echo cd gen