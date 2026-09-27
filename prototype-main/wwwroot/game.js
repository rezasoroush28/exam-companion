(() => {
 'use strict';
 const $ = id => document.getElementById(id);
 const chords = [
  {name:'Cmaj7',notes:['C4','E4','G4','B4']},
  {name:'Dm7',notes:['D4','F4','A4','C5']},
  {name:'Em7',notes:['E4','G4','B4','D5']},
  {name:'Fmaj7',notes:['F4','A4','C5','E5']},
  {name:'G7',notes:['G4','B4','D5','F5']}
 ];
 const player = new GramophoneGuitarPlayer($('character-scene'), {onUpdate: p => {
  const captions={entrance:'نوازنده در راه است…',short:'یک نت تازه!',long:'آکورد فصل',frustration:'کمی استراحت…'};
  $('motion-label').textContent=p.playing?(captions[p.clip]||''):'هر پاسخ، یک قدم تا آهنگ تو';
 },onComplete: clip => {if(clip==='entrance'&&entering)finishEntrance();}});
 const audio = new GuitarAudio.GuitarSynth();
 let state,lesson,feedback=null,selected=null,busy=false,sound=true,generation=0,entering=false;
 function gateEntrance(active){
  document.querySelectorAll('.topbar,.chapter,.music-strip,.album-heading,.topics,.learning,footer')
   .forEach(el=>el.inert=active);
 }
 function beginEntrance(){
  entering=true;gateEntrance(true);
  // Prepare the first offstage pose before exposing the SVG. Explicitly
  // requested startup motion also runs when the OS uses reduced motion.
  player.play('entrance',null,true);
  document.body.dataset.entry='playing';
  if(document.hidden)player.pause();
 }
 function finishEntrance(){
  entering=false;document.body.dataset.entry='done';gateEntrance(false);
 }
 function node(tag,text,cls){const el=document.createElement(tag);if(text!==undefined)el.textContent=text;if(cls)el.className=cls;return el;}
 function button(text,action,cls){const b=node('button',text,cls);b.type='button';b.addEventListener('click',action);return b;}
 function notify(message){$('notice').hidden=!message;$('notice').textContent=message||'';}
 async function api(url,body,method=null){
  const options=method?{method}:body===undefined?{}:{method:'POST'};
  if(body!==undefined){options.headers={'Content-Type':'application/json'};options.body=JSON.stringify(body);}
  const response=await fetch('/api/'+url,options);
  const value=response.status===204?null:await response.json();if(!response.ok)throw new Error(value?.error||'دوباره تلاش کنید.');return value;
 }
 function stop(){generation++;audio.stopMusic();player.ready();}
 async function play(order,noteIndex=null){
  stop(); const token=generation;
  try {
   let schedule;
   if(sound){await audio.initializeAudio();if(token!==generation)return;
    schedule=noteIndex===null?audio.playGuitarChord(chords[order].notes,2.4):audio.playGuitarNote(chords[order].notes[noteIndex%4],.72);
   }else{const start=performance.now(),duration=noteIndex===null?2.4:.72;schedule={start:0,starts:[0],duration,elapsed:()=> (performance.now()-start)/1000};}
   if(noteIndex===null)player.playLongGuitarAnimation(schedule);else player.playShortGuitarAnimation(schedule);
   if(schedule.finished){await schedule.finished;if(token===generation)player.ready();}
  }catch{if(token===generation){player.play(noteIndex===null?'long':'short',null,true);$('motion-label').textContent='برای فعال‌کردن صدا، شنیدن فصل را بزنید.';}}
 }
 function current(){return state.topics.find(t=>t.id===state.currentTopicId);}
 function render(){
  if(!state)return;
  const topic=feedback?state.topics.find(t=>t.order===feedback.topicOrder):current();
  $('topic-name').textContent=topic.name;$('chord').textContent=chords[topic.order].name;
  $('chapter-caption').textContent=state.isRepair?'صیقل‌دادن ملودی':`${topic.completedCycles} از ${topic.requiredCycles} چرخه کامل شده`;
  const done=state.topics.filter(t=>t.completedCycles===t.requiredCycles).length;
  $('album-count').textContent=`${done} از ${state.topics.length}`;
  $('notes').replaceChildren(...chords[topic.order].notes.map((n,i)=>{const li=node('li',n);li.dir='ltr';li.classList.toggle('filled',i<topic.notesRevealed||topic.progress===1);return li;}));
  $('topics').replaceChildren(...state.topics.map(t=>{
   const tile=node('article',undefined,'topic-tile');tile.style.setProperty('--fill',`${t.progress*100}%`);tile.classList.toggle('active',t.id===state.currentTopicId);
   tile.append(node('small',`${t.order+1} / ${chords[t.order].name}`),node('strong',t.name),node('span',t.status==='Locked'?'قفل':t.hasScratch?'نیاز به صیقل':t.progress===1?'کامل شده':`${Math.round(t.progress*100)}٪`));
   if(t.status==='CompletedWithScratch'&&!state.isRepair&&!feedback){const repair=button('صیقل ملودی',()=>repairTopic(t.id));repair.disabled=busy;tile.append(repair);}return tile;
  }));
  const card=$('question-card');card.setAttribute('aria-busy',String(busy));card.replaceChildren();
  if(feedback){
   card.append(node('span',feedback.correct?'درست بود!':'یک فرصت برای یادگیری','badge'),node('h2',feedback.message));
   card.append(node('p',`گزینه درست: ${feedback.correctOption}`));
   if(feedback.explanation)card.append(node('p',feedback.explanation,'explanation'));
   card.append(button('ادامه',()=>{feedback=null;sessionStorage.removeItem('guitar-feedback');selected=null;render();$('question-card').focus();},'primary'));
  }else if(state.status==='Completed'&&!state.isRepair){
   card.append(node('span','آلبوم کامل شد','badge'),node('h2','پنج فصل، یک آهنگ از تو'),node('p','همه فصل‌ها را ساختی. می‌توانی آلبومت را بشنوی یا ملودی‌های خش‌دار را صیقل بدهی.'));
   card.append(button('پخش آلبوم',playAlbum,'primary'));
  }else if(state.question){
   const q=state.question;
   card.append(node('span',state.isRepair?'تمرین ترمیم':q.type==='Challenge'?'چالش پایان چرخه':'یادگیری','badge'));
   card.append(node('small',`چرخه ${state.currentCycle} از ${state.requiredCycles} · ${state.educationalAnswered} از ${state.educationalTarget} سؤال آموزشی`,'question-meta'));
   card.append(node('h2',q.text));
   const options=node('div',undefined,'choices');options.setAttribute('role','group');options.setAttribute('aria-label','گزینه‌های پاسخ');
   q.options.forEach((o,i)=>{const b=button('',()=>{selected=o.key;render();$('option-'+o.key).focus();},'choice');b.id='option-'+o.key;b.disabled=busy;b.setAttribute('aria-pressed',String(selected===o.key));b.append(node('span',String(i+1),'option-number'),node('span',o.text));if(q.developerCorrectOption===o.key)b.append(node('span','پاسخ درست · توسعه','correct-tag'));options.append(b);});card.append(options);
   if(q.developerCorrectOption){const details=node('details',undefined,'developer');details.append(node('summary','ابزار توسعه'),node('p','گزینه درست: '+q.developerCorrectOption));card.append(details);}
   const submit=button(busy?'در حال ثبت…':'ثبت پاسخ',submitAnswer,'primary');submit.id='submit';submit.disabled=!selected||busy;card.append(submit);
  }
  $('new-game').disabled=busy;$('listen').disabled=busy;
 }
 async function submitAnswer(){
  if(busy||!selected||feedback)return;
  busy=true;notify('');const q=state.question,turn=state.turnId,topic=current(),option=selected;
  // Unlock during the gesture; correctness still comes exclusively from the server.
  if(sound)audio.initializeAudio().catch(()=>{});
  render();
  try{
   feedback=await api(`sessions/${state.sessionId}/answers`,{questionId:q.id,turnId:turn,option});state=feedback.state;
   sessionStorage.setItem('guitar-feedback',JSON.stringify(feedback));
   if(feedback.correct)play(topic.order,q.type==='Challenge'?null:topic.notesRevealed);
   else {stop();$('motion-label').textContent='اشکالی ندارد؛ با هم ادامه می‌دهیم.';}
  }catch(e){notify(e.message);}finally{busy=false;render();}
 }
 async function repairTopic(id){if(busy)return;busy=true;stop();render();try{state=await api(`sessions/${state.sessionId}/repairs`,{topicId:id});selected=null;}catch(e){notify(e.message);}finally{busy=false;render();}}
 async function newGame(){if(busy||entering||!lesson)return;busy=true;stop();notify('');const oldId=state?.sessionId||localStorage.getItem('guitar-session');try{if(oldId)await api(`sessions/${oldId}`,undefined,'DELETE');localStorage.removeItem('guitar-session');sessionStorage.removeItem('guitar-feedback');feedback=null;selected=null;state=await api('sessions',{lessonId:lesson.id});localStorage.setItem('guitar-session',state.sessionId);beginEntrance();}catch(e){notify(e.message);}finally{busy=false;render();}}
 async function playAlbum(){if(!sound){notify('برای شنیدن آلبوم، صدا را روشن کنید.');return;}stop();const token=generation;try{await audio.initializeAudio();for(const topic of state.topics){if(token!==generation)return;const schedule=audio.playGuitarChord(chords[topic.order].notes,2.4);$('chord').textContent=chords[topic.order].name;player.playLongGuitarAnimation(schedule);await schedule.finished;}if(token===generation)player.ready();}catch(e){notify(e.message);}}
 $('sound').addEventListener('click',()=>{sound=!sound;stop();$('sound').textContent=sound?'صدا روشن':'صدا خاموش';$('sound').setAttribute('aria-pressed',String(sound));});
 $('listen').addEventListener('click',()=>state&&play((feedback?state.topics.find(t=>t.order===feedback.topicOrder):current()).order));
 $('new-game').addEventListener('click',newGame);
 $('question-card').tabIndex=-1;
 $('question-card').addEventListener('keydown',e=>{if(feedback||busy||!state?.question||e.target.tagName==='INPUT')return;const i='1234'.indexOf(e.key);if(i>=0&&state.question.options[i]){e.preventDefault();selected=state.question.options[i].key;render();$('option-'+selected).focus();}});
 document.addEventListener('visibilitychange',()=>{if(entering){if(document.hidden)player.pause();else player.resume();}else if(document.hidden)stop();});
 window.addEventListener('pagehide',()=>{stop();audio.dispose();player.destroy();});
 window.addEventListener('pageshow',e=>{if(e.persisted)location.reload();});
 matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change',e=>{player.reduced=e.matches;if(!entering)stop();});
 beginEntrance();
 (async()=>{try{lesson=await api('lesson');const id=localStorage.getItem('guitar-session')||lesson.resumeSessionId;
  state=id?await api('sessions/'+id):await api('sessions',{lessonId:lesson.id});localStorage.setItem('guitar-session',state.sessionId);
  try{const saved=JSON.parse(sessionStorage.getItem('guitar-feedback'));if(saved?.state.sessionId===state.sessionId&&saved.state.turnId===state.turnId)feedback=saved;}catch{}
  render();
 }catch(e){notify(e.message);$('question-card').replaceChildren(button('تلاش دوباره',()=>location.reload(),'primary'));}})();
})();
