using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Deterministic original synthesized score. No external audio or licensing dependency.</summary>
    public sealed class FarmSoundscape : MonoBehaviour
    {
        private PortfolioSession session;
        private readonly AudioSource[] layers=new AudioSource[3];
        private readonly AudioClip[] clips=new AudioClip[3];
        private int active;
        public void Configure(PortfolioSession owner)
        {
            session=owner;
            for(int i=0;i<3;i++)
            {
                clips[i]=Compose(i);
                layers[i]=gameObject.AddComponent<AudioSource>();layers[i].playOnAwake=false;
                layers[i].clip=clips[i];layers[i].loop=true;layers[i].volume=0;layers[i].Play();
            }
            session.PhaseChanged+=Changed;Changed(session.Phase);
        }
        private void Changed(SlicePhase phase)=>active=phase==SlicePhase.Boss||phase==SlicePhase.BossIntro?2:phase==SlicePhase.Night||phase==SlicePhase.Preparation?1:0;
        private void Update()
        {
            for(int i=0;i<3;i++)if(layers[i]!=null)
                layers[i].volume=Mathf.MoveTowards(layers[i].volume,i==active?(i==0?.19f:.16f):0,Time.unscaledDeltaTime*.12f);
        }
        private static AudioClip Compose(int mood)
        {
            const int rate=22050;const float duration=32;
            var samples=new float[(int)(rate*duration)];
            int[] melody=mood==0?new[]{0,7,12,16,14,12,7,4}:new[]{0,7,12,15,7,3,10,7};
            int[] roots=mood==0?new[]{48,53,55,48}:new[]{45,41,43,40};
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate;int chord=(int)(t/8)%4;
                float step=mood==2?.25f:mood==1?.5f:1f;
                float noteTime=t%step;
                int degree=melody[(int)(t/step)%melody.Length];
                float hz=440*Mathf.Pow(2,(roots[chord]+degree-69)/12f);
                float pluck=Mathf.Sin(2*Mathf.PI*hz*noteTime)+.28f*Mathf.Sin(4*Mathf.PI*hz*noteTime);
                pluck*=Mathf.Exp(-noteTime*(mood==0?5:11))*Mathf.Min(1,noteTime*140)*.23f;
                float bassHz=440*Mathf.Pow(2,(roots[chord]-12-69)/12f);
                float pad=Mathf.Sin(t*2*Mathf.PI*bassHz)*.1f;
                float beat=t%(mood==2?.5f:1f);
                float percussion=mood==0?0:Mathf.Sin(2*Mathf.PI*(48*beat+1.8f*(1-Mathf.Exp(-beat*24))))*Mathf.Exp(-beat*20)*.19f;
                float birds=mood==0?Mathf.Sin(2*Mathf.PI*(1400*t+50*Mathf.Sin(t*12)))*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*.8f)),24)*.014f:0;
                float seam=Mathf.Min(1,Mathf.Min(t,duration-t)*10);
                samples[i]=(pluck+pad+percussion+birds)*seam;
            }
            var clip=AudioClip.Create(mood==0?"Morning · original score":mood==1?"Night watch · original score":"The Custodian · original score",samples.Length,1,rate,false);
            clip.SetData(samples,0);return clip;
        }
        private void OnDestroy()
        {
            if(session!=null)session.PhaseChanged-=Changed;
            foreach(var clip in clips)if(clip!=null)Destroy(clip);
        }
    }
}
