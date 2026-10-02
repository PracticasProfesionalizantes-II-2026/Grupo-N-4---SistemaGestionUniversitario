document.addEventListener("DOMContentLoaded", () => {
  Acadion.iniciarPantalla();
  const year = document.getElementById("paymentYear");
  const month = document.getElementById("paymentMonth");
  const message = document.getElementById("paymentMessage");
  const now = new Date(); year.value = now.getFullYear();
  const months = ["Enero","Febrero","Marzo","Abril","Mayo","Junio","Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"];
  months.forEach((name,index)=>month.append(new Option(name,String(index+1)))); month.value=String(now.getMonth()+1);
  const label = value => ({Pendiente:"Pendiente",EnRevision:"Pendiente de revisión",AlDia:"Aprobado",Rechazado:"Rechazado",Exentado:"Exentado",Vencida:"Vencido",Impaga:"Impago"}[value] || "Pendiente");
  function state(id,value){const node=document.getElementById(id);node.textContent=label(value);node.dataset.state=value||"Pendiente";}
  async function load(){
    message.textContent="Cargando…";
    try{
      const data=await AcadionApi.request(`/api/me/pagos?anio=${year.value}&mes=${month.value}`);
      document.getElementById("feeTitle").textContent=`Cuota de ${months[Number(month.value)-1]} ${year.value}`;
      state("enrollmentStatus",data.matricula?.estado); state("feeStatus",data.cuota?.estado);
      const enrollmentFile=document.getElementById("enrollmentFile"); enrollmentFile.hidden=!data.matricula?.tieneComprobante; enrollmentFile.href=`${AcadionApi.baseUrl}/api/me/pagos/matricula/${year.value}/comprobante`;
      const feeFile=document.getElementById("feeFile"); feeFile.hidden=!data.cuota?.tieneComprobante; feeFile.href=`${AcadionApi.baseUrl}/api/me/pagos/cuotas/${year.value}/${month.value}/comprobante`;
      message.textContent="";
    }catch(error){message.className="status-message error";message.textContent=error.message;}
  }
  async function submit(form,path){
    const button=form.querySelector("button[type=submit]"); button.disabled=true; message.textContent="Enviando comprobante…";
    try{const result=await AcadionApi.request(path,{method:"POST",body:new FormData(form)});message.className="status-message success";message.textContent=result.mensaje;form.querySelector('input[type="file"]').value="";await load();}
    catch(error){message.className="status-message error";message.textContent=error.message;}finally{button.disabled=false;}
  }
  document.getElementById("enrollmentForm").addEventListener("submit",event=>{event.preventDefault();submit(event.currentTarget,`/api/me/pagos/matricula/${year.value}/comprobante`);});
  document.getElementById("feeForm").addEventListener("submit",event=>{event.preventDefault();submit(event.currentTarget,`/api/me/pagos/cuotas/${year.value}/${month.value}/comprobante`);});
  document.getElementById("loadPayments").addEventListener("click",load); month.addEventListener("change",load); load();
});
