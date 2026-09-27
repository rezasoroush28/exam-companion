const assert=require('node:assert/strict');
const {DatabaseSync}=require('node:sqlite');
const path=require('node:path');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
(async()=>{
 const root='http://127.0.0.1:5197';const browser=await chromium.launch({channel:'msedge',headless:true});
 const db=new DatabaseSync(path.resolve(__dirname,'../data/guitar-game.db'));
 let session;
 try{
  const context=await browser.newContext({viewport:{width:390,height:844}});
  const lesson=await (await context.request.get(root+'/api/lesson')).json();assert.equal(lesson.topics.length,5);
  session=await (await context.request.post(root+'/api/sessions',{data:{lessonId:lesson.id}})).json();
  await context.addInitScript(id=>localStorage.setItem('guitar-session',id),session.sessionId);
  const page=await context.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(root);
  await page.waitForFunction(()=>document.querySelector('#locomotion-root').getAttribute('transform')!=='translate(0 0)');
  await page.waitForSelector('.choice');
  for(const width of [360,390,768,1024,1440]){await page.setViewportSize({width,height:900});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));}
  await page.setViewportSize({width:390,height:844});
  await page.waitForFunction(()=>document.querySelector('#character-scene').dataset.performance==='ready');
  await page.screenshot({path:path.resolve(__dirname,'../artifacts/mobile.png'),fullPage:true});
  let count=0;
  while(session.status!=='Completed'&&count<40){
   const q=session.question;const key=db.prepare('SELECT CorrectOption FROM Questions WHERE Id=?').get(q.id.toUpperCase()).CorrectOption;
   await page.locator('#option-'+key).click();
   const response=page.waitForResponse(r=>r.url().endsWith('/answers'));
   await page.locator('#submit').click();const result=await (await response).json();assert.equal(result.correct,true);
   const old=session;session=result.state;
   await page.waitForFunction(()=>document.querySelector('#question-card').textContent.includes('درست بود'));
   await page.waitForFunction(clip=>document.querySelector('#character-scene').dataset.performance===clip,q.type==='Challenge'?'long':'short');
   if(count===0){
    const duplicate=await context.request.post(root+`/api/sessions/${session.sessionId}/answers`,{data:{questionId:q.id,turnId:old.turnId,option:key}});assert.equal(duplicate.status(),409);
    await page.reload();await page.waitForFunction(()=>document.querySelector('#question-card').textContent.includes('درست بود'));
   }
   await page.getByRole('button',{name:'ادامه',exact:true}).click();count++;
  }
  assert.equal(session.status,'Completed');assert.equal(session.topics.filter(t=>t.progress===1).length,5);
  await page.getByRole('button',{name:'پخش آلبوم',exact:true}).waitFor();
  assert.deepEqual(errors,[]);console.log('PASS: five-topic album, '+count+' answers, single-note/chord motion, duplicate rejection, feedback resume, 360–1440px layouts');
 }finally{
  await browser.close();
  if(session?.sessionId){const id=session.sessionId.toUpperCase();db.exec('PRAGMA foreign_keys=ON');db.prepare('DELETE FROM Sessions WHERE Id=?').run(id);}
  db.close();
 }
})().catch(e=>{console.error(e);process.exitCode=1;});
