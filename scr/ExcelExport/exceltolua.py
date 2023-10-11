#!/usr/bin/python
# -*- coding: UTF-8 -*-
# TODO: field 重复检查

import sys
import os
import traceback
import xlrd
import re

# 当前脚本路径
curpath = os.path.dirname(os.path.abspath(sys.argv[0]))
print("curpath",curpath,sys.argv[1])

# excelPath =curpath + "./炼体.xlsx"
# excelPath =curpath + "/lianti.xlsx"

LUA_CONF_TEMPLATE_HEAD = """---Filename: DB_%s.lua
---Author: auto-created by data gen tool.
---@class DB_%s
%slocal DB_%s = {}
"""

LUA_CONF_TEMPLATE_KEYS = """
local keys = {
    %s
}
"""

LUA_CONF_TEMPLATE_FUNC = """
local mt = {}
mt.__index = function (table, key)
    for i = 1, #keys do
        if (keys[i] == key) then
            return table[i]
        end
    end
end

mt.__newindex = function(table, key, value)
    print("lua Error : Attempt to update a read-only table!!! key = "..key)
end

---@return DB_%s  取对应ID数据
function DB_%s.getCfgByIdx(key_id)
    local id_data = %s[key_id]
    if id_data == nil then
        return nil
    end
    if getmetatable(id_data) ~= nil then
        return id_data
    end
    setmetatable(id_data, mt)

    return id_data
end

---@return DB_%s[]      取同一参数，相同数值数据
function DB_%s.getArrDataByField(fieldName, fieldValue)
    local arrData = {}
    local fieldNum = 1
    for i=1, #keys do
        if keys[i] == fieldName then
            fieldNum = i
            break
        end
    end
    for k, v in pairs(%s) do
        if v[fieldNum] == fieldValue then
            setmetatable (v, mt)
            arrData[#arrData+1] = v
        end
    end

    return arrData
end

---@return DB_%s[]
function DB_%s.getAllCfgData()
    for k, v in pairs(%s) do
        setmetatable (v, mt)
    end
    return %s
end

return DB_%s
"""

# 参数；去除
def restStr(str):
    if str.find(";",0) > -1:
        newStr = str.split(";",1)
        return newStr[1]
    else:
        return str

def get_default_val(c_type):
    d_val = ""
    if c_type == "string":
        d_val = "\"\""
    elif c_type == "int" or c_type == "float":
        d_val = 0
    elif c_type == "bool":
        d_val = "false"
    else:
        d_val = "{}"
    return d_val

def is_float(num):
    point_num = num.count(".")
    if point_num > 1:
        return False
    num = num.replace(".", "")
    return is_int(num)

def is_int(num):
    if num.startswith("-"):
        num = num.lstrip("-")
    return num.isdigit()

def is_number(s):
    try:
        float(s)
        return True
    except ValueError:
        pass
 
    try:
        import unicodedata
        unicodedata.numeric(s)
        return True
    except (TypeError,ValueError):
        pass
 
    return False

def changeStr(str):
    if is_number(str):
        return "%d" %int(str)
    else:
        return '"%s"' %str

def parse_type(cell_val_type, cell):
    d_val = get_default_val(cell_val_type)
    # print("cell_val_type",cell,cell_val_type)
    val = ""
    if cell_val_type == 'string':
        if cell.ctype == 0:
            val = d_val
        else:
            val = str(cell.value)
            if cell.ctype == 2 and cell.value % 1 == 0:
                val = str(int(cell.value))

            val = "\"%s\"" %(val)
    elif cell_val_type == 'int':
        if cell.ctype == 0:
            val = d_val
        else:
            val = int(cell.value)
    elif cell_val_type == 'float':
        if cell.ctype == 0:
            val = d_val
        else:
            val = float(cell.value)
    elif cell_val_type == 'bool':
        if cell.ctype == 0:
            val = d_val
        else:
            val = cell.value
    else:
        # 空值强转类型0
        if not cell.value:
            cell.ctype = 0
            
        if cell.ctype == 0:
            val = d_val
        else:
            # 数组类型
            sp_arr = cell_val_type.split("[")
            t_type = sp_arr[0]
            dim_num = cell_val_type.count("[")
            if dim_num > 2 or dim_num <= 0:
                return "", "invalid type:%s" % cell_val_type

            val = cell.value
            if cell.ctype == 2 and cell.value % 1 == 0:
                val = str(int(cell.value))
            if dim_num == 1:
                # 一维数组
                lua_val ="{"
                temp_arr = val.split("|")
                tidx = 0
                for k in range(len(temp_arr)):
                    tidx = tidx + 1
                    re_val = changeStr(temp_arr[k])
                    # print("re_val",tidx,re_val,re_val)
                    if tidx > 1:
                        re_val =  ",%s" % re_val
                    lua_val += re_val
                lua_val +="}"
                val = lua_val
            else:
                # 二维数组
                lua_val ="{"
                temp_arr = val.split("|")
                tidx = 0
                for k in range(len(temp_arr)):
                    tidx = tidx + 1
                    temp = temp_arr[k].split("#")
                    re_key = temp[0]
                    if len(temp)==1:
                        re_val = 1
                    else:
                        if len(temp[1]) ==0:
                            re_val = 1
                        else:
                            re_val = temp[1]

                    re_key = changeStr(re_key)
                    re_val = changeStr(re_val)
                    lastTable = "{%s,%s}"% (re_key , re_val)

                    if tidx > 1:
                        lastTable =  ",%s" % lastTable
                    lua_val += lastTable

                lua_val +="}"
                val = lua_val
    return val,''


def is_contains_chinese(strs):
    for _char in strs:
        if '\u4e00' <= _char <= '\u9fa5':
            return True
    return False

#读取xlsx，并导出lua
def exceltoLua(src_excel_path,excelName,out_lua_path):

    if not src_excel_path:
        print("输入表为空")
        return
    
    file_name = os.path.basename(src_excel_path)
    # print("file_name",file_name)

    excel_data_src = xlrd.open_workbook(src_excel_path, encoding_override = 'utf-8')
    # print('[excel] excel_sheet names : %s'%excel_data_src.sheet_names())
    # print("sheets",sheets)

    curName = ''
    for sheet in excel_data_src.sheet_names():
        if excelName == sheet:
            curName = sheet

    if curName == '':
        print("未找到对应表格.....",excelName)
        exit(1)
    else:
        excel_sheet = excel_data_src.sheet_by_name(curName)

    # 文件导出名
    if "#" in curName:
        fileName = curName.split("#")[1]
    else:
        fileName = curName
    
    if is_contains_chinese(fileName):
        print("导出表文件sheet中含有中文",curName,"    请检查sheet 后再次尝试！")
        exit(0)

    out_lua_path = out_lua_path + "/DB_"+fileName+".lua"

    # excel data dict
    excel_data_dict = {}

    # col name list
    col_name_list = []

    #col val type list
    col_val_type_list = []

    # ctype: 0 empty, 1 string, 2 number, 3 date, 4 boolean, 5 error

    # excel_sheet.ncols
    # print("第一列数据",excel_sheet.col(0))
    # print("第一行数据",excel_sheet.row(0))

    begin = '<>'
    beginRowIdx = int(sys.argv[3])     #第几行开始
    beginColIdx = int(sys.argv[4])     #第几列开始

    # print("beginColIdx",beginColIdx)

    # 第一行是所有列的描述
    col_desc_list = []
    for col in range(beginColIdx, excel_sheet.ncols):
        cell = excel_sheet.cell(beginRowIdx, col)
        # print("第一列数据",cell.value)
        col_desc_list.append(str(restStr(cell.value)))
        assert cell.ctype == 1, "第一列数据found a invalid col name in 有效第几列col [%d] !~" % (col)
    # print("col_desc_list",col_desc_list)

    # 遍历第二行的所有列 保存字段名
    for col in range(beginColIdx, excel_sheet.ncols):
        cell = excel_sheet.cell(beginRowIdx+1, col)
        # print("第二列数据",cell.value)
        col_name_list.append(str(restStr(cell.value)))
        assert cell.ctype == 1, "第二列数据found a invalid col name in 有效第几列col [%d] !~" % (col)
    # print("col_name_list",col_name_list)

    # 遍历第三行的所有列 保存数据类型
    for col in range(beginColIdx, excel_sheet.ncols):
        cell = excel_sheet.cell(beginRowIdx+2, col)
        print("第三列数据",cell.value)
        col_val_type_list.append(str(restStr(cell.value)))
        assert cell.ctype == 1, "第三列数据found a invalid col val type in 有效第几列col [%d] !~" % (col)
    # print("col_val_type_list",col_val_type_list)

    # 剔除表头、字段名和字段类型所在行
    # 从第四行开始遍历 构造行数据
    
    # print("excel_sheet.ncols",excel_sheet.ncols)
    check_id_list = []
    csv_content_list = excel_sheet.nrows
    
    currRow = beginRowIdx + 3       #<>起始符号行+3，减去三行定义参数
    for row in range(currRow,csv_content_list):
        # 保存数据索引 默认第一列为id
        cell_id = excel_sheet.cell(row, beginColIdx)
        if cell_id.ctype == 0:
            print("Id 缺失,请检查数据表")
            exit(2)

        # 忽略分号注释开头 ;
        if str(cell_id.value).startswith(";"):
            continue
        # assert cell_id.ctype == 2, "found a invalid id in row [%d] !~" % (row)
        # 检查id的唯一性
        if cell_id.value in excel_data_dict:
            print('[warning] duplicated data id: "%d", all previous value will be ignored!~' % (cell_id.value))

        # row data list
        row_data_list = []

        # 保存每一行的所有数据
        # print("col_val_type_list",col_val_type_list[0])

        ID_type = col_val_type_list[0]
        if ID_type == "int" :
            val_id = int(cell_id.value)
        elif ID_type == "string":
            cell_val =  cell_id.value
            if cell_id.ctype == 2 and cell_id.value % 1 == 0:
                cell_val = int(cell_id.value)
            val_id = str(cell_val)
        elif cell_id.ctype == 0:
            print("***********ID有缺失")
        else:
            print("***********ID 不符合规范，使用int or string")

        # 参数名
        # if cell_id.ctype == 2:
        #     val_id = int(cell_id.value)
        # else:
        #     val_id = str(cell_id.value)
        
        # 保存有效数据
        dd = 0
        row_data_list = []
        # print("beginColIdx, excel_sheet.ncols",beginColIdx, excel_sheet.ncols,excel_sheet.cell(row, col-beginColIdx-1))
        for col in range(beginColIdx, excel_sheet.ncols):
            cell = excel_sheet.cell(row, col)
            k = col_name_list[col-beginColIdx]
            cell_val_type = col_val_type_list[col-beginColIdx]
            # print("行，列",row, col,cell_val_type,cell.value)
            # print("cell",row,col,cell,col-beginColIdx,k,cell_val_type)
            # ignored the string that start with '_'
            # 忽略下划线开头_
            if str(k).startswith('_'):
                continue

            dd = dd +1
            res ,err = parse_type(cell_val_type,cell)
            row_data_list.append(res)

        excel_data_dict[val_id] = row_data_list
        check_id_list.append(val_id)

    # 正则搜索lua文件名 不带后缀 用作table的名称 练习正则的使用
    searchObj = re.search(r'([^\\/:*?"<>|\r\n]+)\.\w+$', out_lua_path, re.M|re.I)
    lua_table_name = searchObj.group(1)
    # print('正则匹配:', lua_table_name, searchObj.group(), searchObj.groups())

    table_name = lua_table_name.split("_")[1]
    field_info = ''
    # col_desc_list
    # col_name_list
    # col_val_type_list

    field_info = ""
    # field
    for i in range(len(col_name_list)):
        field = col_name_list[i]
        if str(field).startswith('_'):
                continue
        cell_val_type = col_val_type_list[i]
        desc = col_desc_list[i]
        field_info += ("---@field %s %s @%s\n" % (field, cell_val_type, desc))
    lua_reader_info = LUA_CONF_TEMPLATE_HEAD % (table_name, table_name, field_info, table_name)

    #keys
    str_keys = ""
    for i in range(len(col_name_list)):
        key = col_name_list[i]
        if str(key).startswith('_'):
            continue
        str_keys += ('"%s",') % key
        
        pass

    lua_reader_info += LUA_CONF_TEMPLATE_KEYS % (str_keys)
    
    #body
    lua_reader_body = LUA_CONF_TEMPLATE_FUNC % (table_name, table_name, table_name, table_name, table_name, table_name, table_name,table_name, table_name, table_name,table_name)
    
    # export to lua file
    lua_export_file = open(out_lua_path, 'w+',encoding='utf-8')
    lua_export_file.write(lua_reader_info)
    
    lua_export_file.write('local %s = {\n' % table_name)
    for k, v in excel_data_dict.items():
        defaultId = '    [%d] = {'          #ID跟随int or String，
        if type(k) == str:
            defaultId = '    ["%s"] = {'
        lua_export_file.write(defaultId % k)

        idx = 0             #写入序号
        maxNum = len(v)     #总长度
        
        for row_data in v:
            idx = idx + 1
            if idx == maxNum:
                rStr = '{0}'.format(row_data)
            else:
                rStr = '{0},'.format(row_data) 
            
            lua_export_file.write(rStr)
        lua_export_file.write('},\n')

    lua_export_file.write('}\n')

    lua_export_file.write(lua_reader_body)

    lua_export_file.close()

    print('[excel] %d row data exported!~' % (excel_sheet.nrows-beginRowIdx))
    print("************打表成功**********")

def main():
    try:
        if len(sys.argv) < 6:
            print('参数不全,xlsx路径, 工作表名字（#**），起始点(X,Y),输出路径',sys.argv)
            exit(1)
        excelPath = os.path.join(curpath, sys.argv[1])
        excelName = sys.argv[2]
        outPath = sys.argv[5]
        # exceltoLua(os.path.join(curpath, sys.argv[1]),os.path.join(curpath, sys.argv[2]))
        # print("入参",excelPath,excelName,os.path.join(curpath,outPath))
        exceltoLua(excelPath,excelName,os.path.join(curpath,outPath))

        # excelName = "炼体#Exercise"
        # outPath = "E:\gen"
        # exceltoLua(excelPath,excelName,outPath)
    except Exception as e:
        print(e)
        print('</br>')
        traceback.print_exc()
        return


# begin
if __name__ == "__main__":
    
    sys.exit(main())

