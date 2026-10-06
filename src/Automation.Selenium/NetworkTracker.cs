using OpenQA.Selenium;
namespace Automation.Selenium;

public static class NetworkTracker
{
    // Install after navigation and BEFORE the action under test. Does not observe earlier requests,
    // service workers, websockets, other frames, or navigations. Reinstall after every navigation.
    public static void Install(IWebDriver driver) => ((IJavaScriptExecutor)driver).ExecuteScript("""
        if (!window.__automationTrackerInstalled) {
          window.__automationTrackerInstalled=true; window.__automationPendingRequests=0;
          const fetch=window.fetch;
          window.fetch=function(...args){
            window.__automationPendingRequests++;
            return Promise.resolve().then(()=>fetch.apply(this,args)).finally(()=>window.__automationPendingRequests--);
          };
          const send=XMLHttpRequest.prototype.send;
          XMLHttpRequest.prototype.send=function(...args){
            window.__automationPendingRequests++;
            let done=false;
            const finish=()=>{if(!done){done=true;window.__automationPendingRequests--;}};
            this.addEventListener('loadend',finish,{once:true});
            try{return send.apply(this,args);}catch(e){finish();throw e;}
          };
        }
        """);
}
