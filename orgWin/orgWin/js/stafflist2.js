//GLOBALS
var template, body;
var odata = [];  //orgs
var pdata = []; //persons
var fdata = []; //filtered persons
var selector, searcher, exporter;	

var templater = new (function(){
  var cache = {};
   
  this.tmpl = function tmpl(str, data){
    var fn = !/\W/.test(str) ?
      cache[str] = cache[str] ||
        tmpl(document.getElementById(str).innerHTML) :
      new Function("obj",
        "var p=[],print=function(){p.push.apply(p,arguments);};" +       
        // Introduce the data as local variables using with(){}
        "with(obj){p.push('" +         
        // Convert the template into pure JavaScript
        str
          .replace(/[\r\t\n]/g, " ")
          .split("<%").join("\t")
          .replace(/((^|%>)[^\t]*)'/g, "$1\r")
          .replace(/\t=(.*?)%>/g, "',$1,'")
          .split("\t").join("');")
          .split("%>").join("p.push('")
          .split("\r").join("\\'")
      + "');}return p.join('');");
     
    return data ? fn( data ) : fn;
  };
})();

function trottle(str) {
		str = str.toUpperCase();
		fdata = pdata.filter(function(a){
			var q = selector.options[selector.selectedIndex].value.indexOf(a.pid) >= 0
			var qq = (a.name.toUpperCase().indexOf(str) != -1 || a.mlname.toUpperCase().indexOf(str) != -1)
			return q && qq;
		});
		body.innerHTML = template(fdata);		
};
	
function load(){
	template = templater.tmpl("templateText");
	body =document.getElementById('container');
	searcher = document.getElementById('searcher');	
    selector = document.getElementById('selector');
	exporter = document.getElementById('exporter');
	
	for(var i=0;i<orgData.length;i++){  //разделяем на персоны и подразделения
		var d = orgData[i];
		if(d.type == "org") {
			odata.push(d);
		}else {
			d.department = orgData.find(function(a){return a.id ==d.pid}).name;
			pdata.push(d);
		}
	}
	fdata = pdata.sort(function compare( a, b ) { //персоны по алфавиту
		return ( a.name < b.name )? -1 : (( a.name > b.name )? 1: 0);
	});
	body.innerHTML = template(fdata);
		
	var roots = odata.filter(function(a){  //корневые организации - у которых нет родителя (pid есть)
		var f1 = function(b){return b.id == a.pid;};
		return odata.find(f1) == null;
	});
	
	var map = odata.reduce(function(prev, cur, index, arr){ //массив в вид объекта
// childs-массив собственных детей, allChilds - строка/конкатенация ИДов всех нижлежащих плюс свой ИД
// по allChilds фильтруются персоны при выборе подразделения - проверяется, что pid персоны содержится в allChilds
		prev[cur.id] = {name: cur.name, depth:0, childs:[], allChilds: cur.id};
		return prev;
	},{});
	
	odata.forEach(function trav(a){
		var pid = a.pid;
		var f2 = function(b){return b.id == pid;};
		var p;
		do{
			p = odata.find(f2);
			if(p){                                    //перебираем родителей
				map[a.id].depth++;                    //глубина текущего
				map[p.id].allChilds += a.id +" ";     //добавляем себя в строку-фильтр детей каждому вышележащему 
				pid = p.pid
			}
		}while(p);
		
		var ch = odata.filter( function(b){return b.pid == a.id;}) //ищем своих непосредственных детей
		if(ch && ch.length){
			map[a.id].childs = ch;                    //добавляем себе в МАССИВ-список детей
		}
	});
	
	var opts = [];	
	roots.forEach(function ch(a){
		var m = map[a.id];
		opts.push({name: "-".repeat(m.depth)+m.name, ch: m.allChilds});
		if(m.childs && m.childs.length){
			m.childs.forEach(ch);
		}
	});
	opts.forEach(function(a){
		selector.options[selector.options.length] = new Option(a.name, a.ch);
	});
	selector.addEventListener("change", function(){trottle(searcher.value)});
	
	var xhttp = window.XMLHttpRequest ? new XMLHttpRequest() : new ActiveXObject("Microsoft.XMLHTTP");
	xhttp.onreadystatechange = function () {
		if (xhttp.readyState === 4 && xhttp.status === 200) {
		    parsecState = JSON.parse(xhttp.responseText);
			setParsecStatus(parsecState);
		}
	};
	xhttp.open("GET", "parsecStatus.aspx", true);
	xhttp.send();	
}

function setParsecStatus(state){
  for(var id in state){
	var no = document.getElementById(id);
	if(no)
		no.className = state[id].tag;
  }  
  fdata.forEach( function(a){
	  a.state = state[a.id].tag;
  });
}

function toxlsx() {
	if(fdata.length == 0) return;
	var bd = exporter.getElementsByTagName('tbody')[0];
    bd.innerHTML = "";
                
	fdata.forEach( function(a){
		var row = bd.insertRow(bd.rows.length);
		row.insertCell(-1).appendChild(document.createTextNode(a.name +" "+a.mlname));	
		row.insertCell(-1).appendChild(document.createTextNode(a.department));
		row.insertCell(-1).appendChild(document.createTextNode(a.title));
		row.insertCell(-1).appendChild(document.createTextNode(a.office));
		row.insertCell(-1).appendChild(document.createTextNode(a.corp));
		row.insertCell(-1).appendChild(document.createTextNode( a.mob || "" ));
		row.insertCell(-1).appendChild(document.createTextNode(a.mail));
		row.insertCell(-1).appendChild(document.createTextNode(a.room));
    });
	
	var EXPORTTABLE = new TableExport(document.getElementById('exporter'), {
			formats: ['xlsx'],
			exportButtons: false,
			filename: 'staff-export'
	});
	
    var exportData = EXPORTTABLE.getExportData()['exporter']['xlsx'];
    EXPORTTABLE.export2file(exportData.data, exportData.mimeType, exportData.filename, exportData.fileExtension, exportData.merges, exportData.RTL, exportData.sheetname);
}

(function(){
	if (typeof(jQuery)!="undefined") {
		jQuery(load);
	}
	else if (window.addEventListener) {
		window.addEventListener( "load", load, false );
	}
	else if (window.attachEvent) {
		window.attachEvent( "onload", load );
	}
})();
