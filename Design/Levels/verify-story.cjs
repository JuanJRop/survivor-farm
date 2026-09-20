const {chromium}=require('C:/Users/Juan Jose/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL}=require('url');const path=require('path');
(async()=>{const b=await chromium.launch({headless:true,channel:'chrome'});try{
 const p=await b.newPage({viewport:{width:760,height:1000}});const errors=[];p.on('pageerror',e=>errors.push(e.message));
 await p.goto(pathToFileURL(path.join(__dirname,'historia-preview.html')).href);const f=p.frameLocator('iframe');
 await f.locator('#sf-valley-story[data-ready=true]').waitFor();
 for(let i=0;i<6;i++){await f.locator('[data-zone="'+i+'"]').click();if(!(await f.locator('.chapter').textContent()).startsWith(String(i+1)))throw Error('Chapter failed');}
 await f.locator('[data-zone="4"]').click();await f.locator('#sf-valley-story').screenshot({path:path.join(__dirname,'historia-del-valle.png')});
 await p.setViewportSize({width:360,height:1000});
 const overflow=await f.locator('#sf-valley-story').evaluate(el=>el.scrollWidth>el.clientWidth+1);if(overflow)throw Error('Overflow');
 await f.locator('#sf-valley-story').screenshot({path:path.join(__dirname,'historia-mobile.png')});
 if(errors.length)throw Error(errors.join('\n'));console.log('PASS: six chapters, sprites, navigation, mobile layout.');
 }finally{await b.close();}})().catch(e=>{console.error(e);process.exitCode=1;});
