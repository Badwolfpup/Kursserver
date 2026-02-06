import{g as A}from"./index-Cu3HQdYe.js";var g={exports:{}},W=g.exports,R;function M(){return R||(R=1,(function(T,F){(function(m,f){T.exports=f()})(W,(function(){function m(i){if(!(typeof window>"u")){var t=document.createElement("style");return t.setAttribute("media","screen"),t.innerHTML=i,document.head.appendChild(t),i}}var f=function(i,t){return f=Object.setPrototypeOf||{__proto__:[]}instanceof Array&&function(e,r){e.__proto__=r}||function(e,r){for(var n in r)r.hasOwnProperty(n)&&(e[n]=r[n])},f(i,t)};function C(i,t){f(i,t);function e(){this.constructor=i}i.prototype=t===null?Object.create(t):(e.prototype=t.prototype,new e)}var h=function(){return h=Object.assign||function(t){for(var e,r=1,n=arguments.length;r<n;r++){e=arguments[r];for(var o in e)Object.prototype.hasOwnProperty.call(e,o)&&(t[o]=e[o])}return t},h.apply(this,arguments)};m(`#editor-resizer {
  position: absolute;
  border: 1px dashed #fff;
  background-color: rgba(0, 0, 0, 0.5);
}
#editor-resizer .handler {
  position: absolute;
  right: -5px;
  bottom: -5px;
  width: 10px;
  height: 10px;
  border: 1px solid #333;
  background-color: rgba(255, 255, 255, 0.8);
  cursor: nwse-resize;
  user-select: none;
}
#editor-resizer .toolbar {
  position: absolute;
  top: -5em;
  left: 50%;
  padding: 0.5em;
  border: 1px solid #fff;
  border-radius: 3px;
  background-color: #fff;
  box-shadow: 0 0 3px rgba(0, 0, 0, 0.5);
  transform: translateX(-50%);
  width: 20em;
}
#editor-resizer .toolbar .group {
  display: flex;
  border: 1px solid #aaa;
  border-radius: 6px;
  white-space: nowrap;
  text-align: center;
  line-height: 2;
  color: rgba(0, 0, 0, 0.65);
}
#editor-resizer .toolbar .group:not(:first-child) {
  margin-top: 0.5em;
}
#editor-resizer .toolbar .group .btn {
  flex: 1 0 0;
  text-align: center;
  width: 25%;
  padding: 0 0.5rem;
  display: inline-block;
  vertical-align: top;
  user-select: none;
  color: inherit;
}
#editor-resizer .toolbar .group .btn:not(:last-child) {
  border-right: 1px solid #bbb;
}
#editor-resizer .toolbar .group .btn:not(.btn-group):active {
  background-color: rgba(0, 0, 0, 0.1);
}
#editor-resizer .toolbar .group .input-wrapper {
  width: 25%;
  border: 1px solid #eee;
  position: relative;
  border-right: 1px solid #bbb;
  min-width: 4em;
}
#editor-resizer .toolbar .group .input-wrapper::after {
  content: " ";
  position: absolute;
  height: 1px;
  background-color: #333;
  left: 0.5em;
  right: 1em;
  bottom: 0.2em;
}
#editor-resizer .toolbar .group .input-wrapper input {
  color: inherit;
  text-align: center;
  width: 100%;
  border: none;
  outline: none;
  padding: 0 0.5em;
  padding-right: 1.5em;
}
#editor-resizer .toolbar .group .input-wrapper input:focus ~ .tooltip {
  display: block;
}
#editor-resizer .toolbar .group .input-wrapper .suffix {
  position: absolute;
  right: 0.5em;
}
#editor-resizer .toolbar .group .input-wrapper .tooltip {
  display: none;
  position: absolute;
  top: 100%;
  left: 0;
  font-size: small;
  background-color: #fff;
  box-shadow: 0 0 3px #a7a7a7;
  padding: 0 0.6em;
  border-radius: 5px;
  zoom: 0.85;
}
`);var k=(function(){function i(t){this.config=h(h({},z),t)}return i.prototype.findLabel=function(t){return this.config?Reflect.get(this.config,t):null},i})(),z={floatLeft:"left",floatRight:"right",center:"center",restore:"restore",altTip:"Press and hold alt to lock ratio!",inputTip:"Press enter key to apply change!"};function L(i){for(var t=[],e=1;e<arguments.length;e++)t[e-1]=arguments[e];return i.replace(/\{(\d+)\}/g,function(r,n){return t.length>n?t[n]:""})}(function(i){C(t,i);function t(){var e=i!==null&&i.apply(this,arguments)||this;return e.originSize=null,e}return t})(HTMLElement);var E=`
<div class="handler" title="{0}"></div>
<div class="toolbar">
  <div class="group">
    <a class="btn" data-type="width" data-styles="width:100%">100%</a>
    <a class="btn" data-type="width" data-styles="width:50%">50%</a>
    <span class="input-wrapper"><input data-type="width" maxlength="3" /><span class="suffix">%</span><span class="tooltip">{5}</span></span>
    <a class="btn" data-type="width" data-styles="width:auto">{4}</a>
  </div>
  <div class="group">
    <a class="btn" data-type="align" data-styles="float:left">{1}</a>
    <a class="btn" data-type="align" data-styles="display:block;margin:auto;">{2}</a>
    <a class="btn" data-type="align" data-styles="float:right;">{3}</a>
    <a class="btn" data-type="align" data-styles="">{4}</a>
  </div>
</div>
`,y=(function(){function i(t,e,r,n){this.resizer=null,this.startResizePosition=null,this.i18n=new k(n?.locale||z),this.options=n,this.resizeTarget=t,t.originSize||(t.originSize={width:t.clientWidth,height:t.clientHeight}),this.editor=r,this.container=e,this.initResizer(),this.positionResizerToTarget(t),this.resizing=this.resizing.bind(this),this.endResize=this.endResize.bind(this),this.startResize=this.startResize.bind(this),this.toolbarClick=this.toolbarClick.bind(this),this.toolbarInputChange=this.toolbarInputChange.bind(this),this.onScroll=this.onScroll.bind(this),this.bindEvents()}return i.prototype.initResizer=function(){var t=this.container.querySelector("#editor-resizer");t||(t=document.createElement("div"),t.setAttribute("id","editor-resizer"),t.innerHTML=L(E,this.i18n.findLabel("altTip"),this.i18n.findLabel("floatLeft"),this.i18n.findLabel("center"),this.i18n.findLabel("floatRight"),this.i18n.findLabel("restore"),this.i18n.findLabel("inputTip")),this.container.appendChild(t)),this.resizer=t},i.prototype.positionResizerToTarget=function(t){if(!(!this.resizer||!this.editor)){var e=t.getBoundingClientRect(),r=this.editor.getBoundingClientRect(),n=e.left-r.left,o=e.top-r.top;this.resizer.style.left=n+"px",this.resizer.style.top=o+"px",this.resizer.style.width=t.clientWidth+"px",this.resizer.style.height=t.clientHeight+"px"}},i.prototype.bindEvents=function(){this.resizer!==null&&(this.resizer.addEventListener("mousedown",this.startResize),this.resizer.addEventListener("click",this.toolbarClick),this.resizer.addEventListener("change",this.toolbarInputChange)),window.addEventListener("mouseup",this.endResize),window.addEventListener("mousemove",this.resizing),this.editor.addEventListener("scroll",this.onScroll)},i.prototype.onScroll=function(){this.positionResizerToTarget(this.resizeTarget)},i.prototype._setStylesForToolbar=function(t,e){var r,n="_styles_"+t,o=this.resizeTarget.style,d=this.resizeTarget[n];o.cssText=o.cssText.replaceAll(" ","").replace(d,"")+(";"+e),this.resizeTarget[n]=e,this.positionResizerToTarget(this.resizeTarget),(r=this.options)===null||r===void 0||r.onChange(this.resizeTarget)},i.prototype.toolbarInputChange=function(t){var e,r=t.target,n=(e=r?.dataset)===null||e===void 0?void 0:e.type,o=r.value;n&&Number(o)&&this._setStylesForToolbar(n,"width: "+Number(o)+"%;")},i.prototype.toolbarClick=function(t){var e,r,n=t.target,o=(e=n?.dataset)===null||e===void 0?void 0:e.type;o&&n.classList.contains("btn")&&this._setStylesForToolbar(o,(r=n?.dataset)===null||r===void 0?void 0:r.styles)},i.prototype.startResize=function(t){var e=t.target;e.classList.contains("handler")&&t.which===1&&(this.startResizePosition={left:t.clientX,top:t.clientY,width:this.resizeTarget.clientWidth,height:this.resizeTarget.clientHeight})},i.prototype.endResize=function(){var t;this.startResizePosition=null,(t=this.options)===null||t===void 0||t.onChange(this.resizeTarget)},i.prototype.resizing=function(t){var e,r,n,o,d,s,a,l,p,v,b;if(this.startResizePosition){var H=t.clientX-this.startResizePosition.left,j=t.clientY-this.startResizePosition.top,u=this.startResizePosition.width,c=this.startResizePosition.height;if(u+=H,c+=j,t.altKey){var w=this.resizeTarget.originSize,O=w.height/w.width;c=O*u}var x=(n=(r=(e=this.options)===null||e===void 0?void 0:e.resizeConstraints)===null||r===void 0?void 0:r.minWidth)!==null&&n!==void 0?n:30,_=(s=(d=(o=this.options)===null||o===void 0?void 0:o.resizeConstraints)===null||d===void 0?void 0:d.minHeight)!==null&&s!==void 0?s:30;u<x&&(u=x),((l=(a=this.options)===null||a===void 0?void 0:a.resizeConstraints)===null||l===void 0?void 0:l.maxWidth)!==void 0&&u>this.options.resizeConstraints.maxWidth&&(u=this.options.resizeConstraints.maxWidth),c<_&&(c=_),((v=(p=this.options)===null||p===void 0?void 0:p.resizeConstraints)===null||v===void 0?void 0:v.maxHeight)!==void 0&&c>this.options.resizeConstraints.maxHeight&&(c=this.options.resizeConstraints.maxHeight),this.resizeTarget.setAttribute("width",""+u),!((b=this.options)===null||b===void 0)&&b.keepAspectRatio||this.resizeTarget.setAttribute("height",""+c),this.positionResizerToTarget(this.resizeTarget)}},i.prototype.destory=function(){this.container.removeChild(this.resizer),window.removeEventListener("mouseup",this.endResize),window.removeEventListener("mousemove",this.resizing),this.editor.removeEventListener("scroll",this.onScroll),this.resizer=null},i})(),I=(function(){function i(t,e){this.element=t,this.cb=e,this.hasTracked=!1}return i})(),P=(function(){function i(){}return i.track=function(t,e){this.iframes.push(new I(t,e)),this.interval||(this.interval=setInterval(function(){i.checkClick()},this.resolution))},i.checkClick=function(){if(document.activeElement){var t=document.activeElement;for(var e in this.iframes)t===this.iframes[e].element?this.iframes[e].hasTracked==!1&&(this.iframes[e].cb.apply(window,[]),this.iframes[e].hasTracked=!0):this.iframes[e].hasTracked=!1}},i.resolution=200,i.iframes=[],i.interval=null,i})();function S(i,t){var e=i.root,r,n;function o(){var d=i.getContents().constructor,s=new d().retain(1);i.updateContents(s)}e.addEventListener("click",function(d){var s,a,l=d.target;d.target&&[!(!((s=t?.disableMediaTypes)===null||s===void 0)&&s.disableImages)&&"img",!(!((a=t?.disableMediaTypes)===null||a===void 0)&&a.disableVideos)&&"video"].includes(l.tagName.toLowerCase())&&i.isEnabled()&&(r=l,n=new y(l,e.parentElement,e,h(h({},t),{onChange:o})))}),i.on("text-change",function(d,s){var a;!(!((a=t?.disableMediaTypes)===null||a===void 0)&&a.disableIframes)&&i.isEnabled()&&e.querySelectorAll("iframe").forEach(function(l){P.track(l,function(){r=l,n=new y(l,e.parentElement,e,h(h({},t),{onChange:o}))})})}),document.addEventListener("mousedown",function(d){var s,a,l,p=d.target;p!==r&&!(!((a=(s=n?.resizer)===null||s===void 0?void 0:s.contains)===null||a===void 0)&&a.call(s,p))&&((l=n?.destory)===null||l===void 0||l.call(n),n=null,r=null)},{capture:!0})}return S}))})(g)),g.exports}var q=M();const $=A(q),N=Object.freeze(Object.defineProperty({__proto__:null,default:$},Symbol.toStringTag,{value:"Module"}));export{N as q};
