function main(){
    var txtGroup = app.activeDocument.layerSets[0].artLayers;
    var counter =1;

    for(var i=0;i<txtGroup.length;i++){
        txtGroup[i].textItem.contents = counter;
        //txtGroup[i].textItem.width = new UnitValue("30 px");
        counter = counter +1;
    }
    alert("done！！！");
}
main();