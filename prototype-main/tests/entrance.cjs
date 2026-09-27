const assert = require('node:assert/strict');
const {chromium} = require(process.env.PLAYWRIGHT_PATH || 'playwright');
(async () => {
 const browser = await chromium.launch({channel:'msedge',headless:true});
 try {
  for(const reducedMotion of ['no-preference','reduce']) for(const width of [390,1440]) {
   const page = await browser.newPage({viewport:{width,height:844}, reducedMotion});
   const errors=[]; page.on('pageerror',e=>errors.push(e.message));
   // Read the catalog but mock session traffic: this visual check writes no player data.
   const lesson = await (await page.request.get('http://127.0.0.1:5197/api/lesson')).json();
   const fixture={sessionId:'entrance-fixture',lessonName:lesson.name,status:'Active',currentTopicId:lesson.topics[0].id,
    currentCycle:1,requiredCycles:1,step:'Educational',turnId:'fixture-turn',question:{id:'fixture-question',type:'Educational',text:'سؤال آزمایشی',options:[{key:'A',text:'گزینه اول'},{key:'B',text:'گزینه دوم'}]},
    topics:lesson.topics, isRepair:false,educationalAnswered:0,educationalTarget:2,challengeAttempts:0};
   await page.route('**/api/sessions**',r=>r.fulfill({json:fixture}));
   for(let load=0;load<2;load++){
    if(load)await page.reload();else await page.goto('http://127.0.0.1:5197/');
    await page.waitForFunction(()=>document.body.dataset.entry==='playing');
    assert.equal(await page.locator('.learning').isVisible(),false);
    assert.equal(await page.locator('.topbar').evaluate(el=>el.inert),true);
    const beforeStage = await page.locator('.stage').boundingBox();
    const beforeSvg = await page.locator('#character-scene').boundingBox();
    const x=await page.locator('#locomotion-root').getAttribute('transform');
    await page.waitForFunction(t=>document.querySelector('#locomotion-root').getAttribute('transform')!==t,x);
    await page.waitForFunction(()=>document.body.dataset.entry==='done');
    const afterStage = await page.locator('.stage').boundingBox();
    const afterSvg = await page.locator('#character-scene').boundingBox();
    for(const key of ['x','y','width','height']) {
     assert.ok(Math.abs(beforeStage[key]-afterStage[key])<.5, 'Stage must not jump: '+key);
     assert.ok(Math.abs(beforeSvg[key]-afterSvg[key])<.5, 'Character scale/frame must not jump: '+key);
    }
    assert.equal(await page.locator('.choice').count(),2);
    assert.equal(await page.locator('.learning').isVisible(),true);
    assert.equal(await page.locator('.topbar').evaluate(el=>el.inert),false);
    assert.equal(await page.locator('#locomotion-root').getAttribute('transform'),'translate(0 0)');
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
   }
   assert.deepEqual(errors,[]);
   console.log('PASS: stable entrance/game frame on first load and reload / '+reducedMotion+' / '+width+'px');
   await page.close();
  }
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
