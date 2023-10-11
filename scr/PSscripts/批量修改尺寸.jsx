function SaveAsJPG(outPath){
    var ad = app.activeDocument;
    savePath = new File(outPath+ad.name);
    var pso = JPEGSaveOptions;
    pso.quality = 5;
    var es=Extension.LOWERCASE;
    ad.saveAs(savePath,pso,true,es);
}

function ChangeSize(){
    var ad = app.activeDocument;
    ad.activeLayer.isBackgroundLayer = false
    ad.activeLayer.positionLocked = false
    //尺寸缩小为原来的75%
    ad.activeLayer.resize(75,75,AnchorPosition.MIDDLECENTER)
    ad.resizeCanvas(ad.width*(0.75),ad.height*(0.75),AnchorPosition.MIDDLECENTER)
}

function AddMapInfo(){
    var ad = app.activeDocument;
    var bg  = ad.artLayers.add();
    var textLayer = ad.artLayers.add();
    textLayer.kind = LayerKind.TEXT;
    //textLayer.textItem.contents="尺寸："+ad.width+"*"+ad.height+"  地图名："+ad.name;
    textLayer.textItem.contents="尺寸："+ad.width/(new UnitValue("9 px"))+"*"+ad.height/(new UnitValue("6 px"))+"  地图名："+ad.name;
    textLayer.textItem.color.rgb = new RGBColor("ffffff");
    textLayer.textItem.size= new UnitValue("60 px")

}

function main(){
    var imageFolder = new Folder(prompt("输入图片所在的文件夹"));
    var files = imageFolder.getFiles("*.bmp")

    var exportPath = prompt("输入输出文件夹")
    for(file in files){
        app.load(files[file]);
        ChangeSize();
        AddMapInfo();
        SaveAsJPG(exportPath+"/");
        app.activeDocument.close(SaveOptions.DONOTSAVECHANGES);
    }
     alert("done!! handle["+files.length+"] images!!!!")
}

main();