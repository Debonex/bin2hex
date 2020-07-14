# bin2hex

1. 上位机编写bin2hex.exe可执行文件，并通过bin2hex.bat脚本 输入：StResource.bin；输出：C3FF01V_Flash.hex

2. 转化过程中，将VerInfo.txt的内容以要求的形式填写至 0x7FFFE0-0x7FFFFB

 (  最大长度28byte   
	0x7FFFE0-0x7FFFEF 为 字符；从 VerInfo.txt 获取版本信息,未填写完成，填写空格
	0x7FFFF0-0x7FFFFB 为 BCD;  从 计算机当前日期获取，并填入，格式 年月日，如：200713 
 )

3. 将0x000000-0x7FFFFB的数据内容通过CRC32进行校验 MSB2LSB