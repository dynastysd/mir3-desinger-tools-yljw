#include"json2.js";

//如果输入文本太多，就不能用prompt
function GetJsonData(){
    var inputData = prompt("输入导出数据"); 
    var jData = JSON.parse(inputData);
    return jData;
}
function GetJsonDataByLocal(){
    var file =File(app.activeDocument.path+"\\config.json")
    file.open("r");
    var jData = JSON.parse(file.read());
    return jData;
}

function SaveAsPNG(appInput,fileName){
    savePath = new File(appInput.path+"\\导出图片\\"+fileName);
    var pso = PNGSaveOptions;
    var es=Extension.LOWERCASE;
    appInput.saveAs(savePath,pso,true,es);
}

function main(){
    var ad = app.activeDocument;
    var textLay = app.activeDocument.artLayers[0];

    var jData = GetJsonDataByLocal();

    const fs = require('fs');
    const folderName = ad.path+'\\导出图片';
    if (!fs.existsSync(folderName)) {
        fs.mkdirSync(folderName);
    } 


    for (var i = 0 ; i< jData.length;i++){
        if (ad.height != new UnitValue("500 px") && ad.width != new UnitValue("500 px") ){
            ad.resizeCanvas(new UnitValue("500 px"),new UnitValue("500 px"))
        }
        //设置文本
        textLay.textItem.contents = jData[i].text;
        //设置尺寸
        textLay.textItem.size = new UnitValue(jData[i].size + " px");
        //设置颜色
        sColor = new SolidColor()
        sColor.rgb.hexValue = jData[i].color;
        textLay.textItem.color = sColor;
        // var sizeX = new UnitValue(textLay.textItem.position[0].value*(-1) + " px") ;
        // var sizeY = new UnitValue(textLay.textItem.position[1].value*(-1) + " px") ;
        // textLay.translate(sizeX,sizeY);
        
        //裁切透明
        ad.trim(TrimType.TRANSPARENT);
        //导出
        SaveAsPNG(ad,jData[i].exportName)
    }
    ad.resizeCanvas(new UnitValue("500 px"),new UnitValue("500 px"))
    alert("done！！！");
}

main();