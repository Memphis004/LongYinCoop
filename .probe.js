const A=Il2Cpp.domain.assembly("Assembly-CSharp");
function arr(l){try{const it=l.field("_items").value,n=l.field("_size").value;const o=[];for(let i=0;i<n;i++){const x=it.get(i);if(x&&!x.handle.isNull())o.push(x)}return o}catch(e){return []}}
const wd=Il2Cpp.gc.choose(A.image.class("GameController"))[0].field("worldData").value;
const hero=wd.method("Player").invoke();const bag=hero.field("itemListData").value;
const items=arr(bag.field("allItem").value).map(i=>{let n=null;try{n=i.field("name").value.content}catch(e){}return [i.field("itemID").value,n]});
let pl=null;try{pl=Il2Cpp.gc.choose(A.image.class("PlotController"))[0]}catch(e){}
JSON.stringify({t:Date.now(),n:items.length,names:items.map(x=>x[1]).sort(),pt:pl?pl.field("plotTextShowing").value:null,pc:pl?pl.field("plotChoiceShowing").value:null},null,1)
