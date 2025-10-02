using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Data;
using System.Text.Json.Serialization;
using webApiIFRS.Models;
using static System.Net.Mime.MediaTypeNames;

namespace webApiIFRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratosController : ControllerBase
    {
        //inyección del contexto de la BD 
        private readonly ConnContext _connContext;
        private readonly ConnContextCTACTE _connCtaCte;
        private readonly ConnContextSEPULTA _connSepulta;
        private readonly ConnContextSICM _connSICM; 
        private readonly ConnContextSICMPBI _connSICMPBI;
        public ContratosController(ConnContext connContext, 
            ConnContextCTACTE connCtaCte, 
            ConnContextSEPULTA connSepulta, 
            ConnContextSICM connSICM, 
            ConnContextSICMPBI connSICMPBI)
        {
            _connContext = connContext;
            _connCtaCte = connCtaCte; 
            _connSepulta = connSepulta;
            _connSICM = connSICM;
            _connSICMPBI = connSICMPBI;
        }
        
        //Lista todos los contratos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contrato>>> GetAllContratos()
        {
            if(_connContext.Contrato == null)
            {
                return NotFound();
            }
            return await _connContext.Contrato.ToListAsync(); 
        }

        //Lista contrato por id y numero de contrato
        [HttpGet("GetContratoByIdyNumCon/{con_id}/{con_num_con}")]
        public async Task<ActionResult<Contrato>> GetContratoByNumCon(int id, string con_num_con)
        {
            if (_connContext.Contrato == null)
            {
                return NotFound();
            }
            var contrato = await _connContext.Contrato
                .FirstOrDefaultAsync(x => x.con_id == id && x.con_num_con == con_num_con); 
            
            if (contrato is null)
            {
                return NotFound();
            }
            return contrato;
        }

        //Lista contrato por numero de contrato
        [HttpGet("GetContratoByNumCon/{con_num_con}")]
        public async Task<ActionResult<Contrato>> GetContratoByNumCon(string con_num_con)
        {
            if (_connContext.Contrato == null)
            {
                return NotFound();
            }
            var contrato = await _connContext.Contrato
                .FirstOrDefaultAsync(x=> x.con_num_con == con_num_con);

            if (contrato is null)
            {
                return NotFound();
            }
            return contrato;
        }

        //lista intereses por devengar de un contrato
        [HttpGet("GetInteresesPorDevByContrato/{int_num_con}")]
        public async Task<ActionResult<InteresesPorDevengar>> GetInteresesPorDevengarDeUnContrato(string int_num_con)
        {
            if(_connContext.InteresesPorDevengar == null)
            {
                return NotFound();
            }
            var intereses = await _connContext.InteresesPorDevengar
                .Where(x=> x.int_num_con == int_num_con)
                .ToListAsync();

            if(intereses is null)
            {
                return NotFound(); 
            }
            return Ok(intereses);
        }

        //lista ingresos diferidos de un contrato
        [HttpGet("GetIngresosDiferidosByContrato/{ing_num_con}")]
        public async Task<ActionResult<IngresosDiferidos>> GetIngresosDiferidosByContrato(string ing_num_con)
        {
            if (_connContext.InteresesPorDevengar == null)
            {
                return NotFound();
            }
            var ingresos = await _connContext.IngresosDiferidos
                .Where(x => x.ing_num_con == ing_num_con)
                .ToListAsync();

            if (ingresos is null)
            {
                return NotFound();
            }
            return Ok(ingresos);
        }


        [HttpPost("ProcesarContratos")]
        public async Task<IActionResult> ProcesarContratos()
        {
            if (_connContext.Contrato == null)
            {
                return NotFound("No hay contratos que procesar");
            }

            DataTable dtContratos = new DataTable();
            DataTable dtPagosRealizados = new DataTable();
            DataTable dtPagosRealizadosTerreno = new DataTable();
            DataTable dtModificaciones = new DataTable();
            DataTable dtFechaPrimerVto = new DataTable();
            DataTable dtInteresPorDev = new DataTable();
            DataTable dtIngresosDiferidos = new DataTable();
            DataTable dtInteresPorDevParaValidar = new DataTable();
            DataTable dtIngresosDiferidosParaValidar = new DataTable();

            DateTime fechaVto = new DateTime();
            DateTime fechaVtoOriginal = new DateTime();
            DateTime fechaUltPagoCuota = new DateTime();
            DateTime fechaUltPagoCuotaMod = new DateTime();

            var interesesPorDevengar = new List<InteresesPorDevengar>();
            var ingresosDiferidos = new List<IngresosDiferidos>();
            double tasaInteres = 2.0 / 100;

            /*OBTENER TODOS LOS CONTRATOS*/
            dtContratos = await _connContext.ListaContratosPorAnio(2025);
            /*VERIFICAR EL ESTADO DE LAS CUOTAS*/
            dtPagosRealizados = await _connContext.ObtenerPagosRealizados(2025);
            dtPagosRealizadosTerreno = await _connContext.ObtenerPagosRealizadosTerreno(2025);
            dtModificaciones = await _connContext.ObtenerModificaciones(2025);
            dtFechaPrimerVto = await _connContext.ObtenerFechaPrimerVctoBov(2025);
            dtInteresPorDevParaValidar = await _connContext.ObtenerInteresPorDev_ListadoContratosYsusCuotas(2025);
            dtIngresosDiferidosParaValidar = await _connContext.ObtenerIngresosDiferidos_ListaCuotas(2025);

            /*PARA SABER CUANTOS REGISTROS SE INSERTARON*/
            int registrosInteresesEsperados = 0;
            int registrosInteresesInsertados = 0;
            int registrosInteresesYaExisten = 0; 
            int registrosIngresosEsperados = 0;
            int registrosIngresosInsertados = 0;
            int registrosIngresosYaExisten = 0; 

            int interesesGuardados = 0;
            int ingresosGuardados = 0;
            int correlativo_int_dev = 0; 

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProcesaContratos.txt");

            using (StreamWriter logWriter = new StreamWriter(logPath, append: true))
            {
                for (int i = 0; i < dtContratos.Rows.Count; i++)
                {
                    correlativo_int_dev = 0;

                    await logWriter.WriteLineAsync($"Se recorre datatable de contratos, Contrato N°: { dtContratos.Rows[i]["con_num_con"].ToString() } - {DateTime.Now}");

                    if (dtContratos.Rows[i]["con_fecha_primer_vcto_ori"] != DBNull.Value)
                    {
                        fechaVto = (DateTime)dtContratos.Rows[i]["con_fecha_primer_vcto_ori"];
                        fechaVtoOriginal = fechaVto;
                    }
                    //busca fecha correcta primer vto
                    DataRow[] fechaPrimer = dtFechaPrimerVto.Select("NUM_CONTRATO='" + dtContratos.Rows[i]["con_num_con"].ToString() + "'");
                    if (fechaPrimer.Length > 0)
                    {
                        fechaVto = (DateTime)fechaPrimer[0]["fecha_vto_cuota"];
                    }

                    //variable tabla de amortización
                    double saldoInicial = Convert.ToInt32(dtContratos.Rows[i]["con_total_credito"]);
                    double valorCuota = Convert.ToInt32(dtContratos.Rows[i]["con_valor_cuota_pactada"]);
                    fechaUltPagoCuota = Convert.ToDateTime(dtContratos.Rows[i]["con_fecha_ingreso"]).Date;

                    //intereses diferidos
                    int interesDiferido = 0;
                    int mesesArriendo = 0;
                    int precioBase = int.Parse(dtContratos.Rows[i]["con_precio_base"].ToString());

                    if (int.Parse(dtContratos.Rows[i]["con_id_tipo_ingreso"].ToString()) == 1 && int.Parse(dtContratos.Rows[i]["con_anos_arriendo"].ToString()) > 0)
                    {
                        mesesArriendo = Convert.ToInt32(dtContratos.Rows[i]["con_anos_arriendo"]) * 12;
                        interesDiferido = precioBase / mesesArriendo;
                    }

                    if (dtInteresPorDev.Columns.Count == 0)
                    {
                        dtInteresPorDev.Columns.Add("ID", typeof(int));
                        dtInteresPorDev.Columns.Add("int_num_con", typeof(string));
                        dtInteresPorDev.Columns.Add("int_correlativo", typeof(int)); 
                        dtInteresPorDev.Columns.Add("int_nro_cuota", typeof(int));
                        dtInteresPorDev.Columns.Add("int_saldo_inicial", typeof(int));
                        dtInteresPorDev.Columns.Add("int_tasa_interes", typeof(int));
                        dtInteresPorDev.Columns.Add("int_cuota_final", typeof(int));
                        dtInteresPorDev.Columns.Add("int_abono_a_capital", typeof(int));
                        dtInteresPorDev.Columns.Add("int_saldo_final", typeof(int));
                        dtInteresPorDev.Columns.Add("int_estado_cuota", typeof(int));
                        dtInteresPorDev.Columns.Add("int_fecha_pago", typeof(DateTime));
                        dtInteresPorDev.Columns.Add("int_fecha_vcto", typeof(DateTime));
                        dtInteresPorDev.Columns.Add("int_fecha_contab", typeof(DateTime));
                        dtInteresPorDev.Columns.Add("int_estado_contab", typeof(int));
                        dtInteresPorDev.Columns.Add("int_tipo_movimiento", typeof(string));
                        dtInteresPorDev.Columns.Add("int_cuotas_pactadas_mod", typeof(int));
                    }

                    if (dtIngresosDiferidos.Columns.Count == 0)
                    {
                        dtIngresosDiferidos.Columns.Add("ID", typeof(int));
                        dtIngresosDiferidos.Columns.Add("ing_num_con", typeof(string));
                        dtIngresosDiferidos.Columns.Add("ing_precio_base", typeof(int));
                        dtIngresosDiferidos.Columns.Add("ing_nro_cuota", typeof(int));
                        dtIngresosDiferidos.Columns.Add("ing_interes_diferido", typeof(int));
                        dtIngresosDiferidos.Columns.Add("ing_fecha_contab", typeof(DateTime));
                        dtIngresosDiferidos.Columns.Add("ing_estado_contab", typeof(int));
                    }

                    correlativo_int_dev++; 
                    for (int i2 = 0; i2 < Convert.ToInt32(dtContratos.Rows[i]["con_cuotas_pactadas"].ToString()); i2++)
                    {
                        int numeroCuota = i2 + 1;

                        await logWriter.WriteLineAsync($"Se recorre el total de cuotas por contrato, cuota n°: {numeroCuota} - {DateTime.Now}");

                        bool existeCuota = dtInteresPorDev.AsEnumerable().Any(row =>
                        row["int_num_con"].ToString() == dtContratos.Rows[i]["con_num_con"].ToString() &&
                        row["int_nro_cuota"].ToString() == numeroCuota.ToString());

                        if (!existeCuota)
                        {
                            DataRow filaNuevaInt = dtInteresPorDev.NewRow();
                            filaNuevaInt["int_num_con"] = dtContratos.Rows[i]["con_num_con"].ToString();
                            filaNuevaInt["int_nro_cuota"] = numeroCuota;
                            filaNuevaInt["int_correlativo"] = correlativo_int_dev; 
                            if (dtContratos.Rows[i]["con_fecha_primer_vcto_ori"] != DBNull.Value)
                            {
                                if (numeroCuota == 1)
                                {
                                    filaNuevaInt["int_fecha_vcto"] = fechaVto.ToShortDateString();
                                    filaNuevaInt["int_fecha_contab"] = GetUltimoDiaDelMes(fechaVto);
                                    filaNuevaInt["int_estado_contab"] = 0;
                                }
                                else
                                {
                                    filaNuevaInt["int_fecha_vcto"] = fechaVtoOriginal.AddMonths(numeroCuota - 1).ToShortDateString();
                                    fechaVto = fechaVtoOriginal.AddMonths(numeroCuota - 1);
                                    filaNuevaInt["int_fecha_contab"] = GetUltimoDiaDelMes(fechaVto);
                                    filaNuevaInt["int_estado_contab"] = 0;
                                }
                            }
                            //revisamos si tiene cuota en terreno y se resta para cuadrar
                            double valorCuotaTerreno = 0;
                            DataRow[] busquedaPagoCuotaTerreno = dtPagosRealizadosTerreno.Select("fecha_vto='" + fechaVto + "' and contrato='" + dtContratos.Rows[i]["con_num_con"].ToString() + "'");
                            if (busquedaPagoCuotaTerreno.Length > 0)
                            {
                                valorCuotaTerreno = Convert.ToInt32(busquedaPagoCuotaTerreno[0]["valor_cuota"]);
                            }

                            //vemos si la cuota ya fue pagada
                            DataRow[] busquedaPagoCuota = dtPagosRealizados.AsEnumerable()
                                                         .Where(r =>
                                                             r.Field<DateTime>("fecha_vcto") == fechaVto &&
                                                             r.Field<string>("contrato") == dtContratos.Rows[i]["con_num_con"].ToString() &&
                                                             r.Field<int>("valor_cuota") == (valorCuota - valorCuotaTerreno)
                                                         //r.Field<int>("numero_cuota") == numeroCuota
                                                         ).ToArray();
                            if (busquedaPagoCuota.Length > 0)
                            {
                                filaNuevaInt["int_fecha_pago"] = busquedaPagoCuota[0]["fecha_pago"];
                                filaNuevaInt["int_estado_cuota"] = 2; //"Pagado";
                                fechaUltPagoCuota = (DateTime)busquedaPagoCuota[0]["fecha_pago"];

                                //se agregan las modificaciones en caso de haberlas
                                var busqueda = dtModificaciones.Select("numero_contrato='" + dtContratos.Rows[i]["con_num_con"] + "'");  //+ " and tipo_sistema=" + dtContratos.Rows[i]["tipo_sistema"]);
                                foreach (DataRow busquedaModificacion in busqueda)
                                {
                                    //fecha de modificación no puede ser mayor a la fecha de pago 
                                    if ((DateTime)busquedaPagoCuota[0]["fecha_pago"] >= (DateTime)busquedaModificacion["fecha_modificacion"])
                                    {
                                        filaNuevaInt["int_estado_cuota"] = 1; //"Pendiente";
                                    }
                                    else
                                    {
                                        filaNuevaInt["int_fecha_pago"] = busquedaPagoCuota[0]["fecha_pago"];
                                        filaNuevaInt["int_estado_cuota"] = 2; //"Pagado";
                                        fechaUltPagoCuota = (DateTime)busquedaPagoCuota[0]["fecha_pago"];
                                    }
                                }
                            }
                            else
                            {
                                filaNuevaInt["int_estado_cuota"] = 1; //"Pendiente";
                            }

                            //calculo de intereses
                            filaNuevaInt["int_saldo_inicial"] = saldoInicial;
                            double interes = 0;
                            if (Convert.ToInt32(dtContratos.Rows[i]["con_cuotas_pactadas"].ToString()) > 3)
                            {
                                interes = saldoInicial * 0.02;
                            }
                            else
                            {
                                interes = 0;
                            }
                            double montoCapital = valorCuota - interes;
                            filaNuevaInt["int_tasa_interes"] = (int)interes;
                            filaNuevaInt["int_cuota_final"] = (int)valorCuota;
                            filaNuevaInt["int_abono_a_capital"] = (int)montoCapital;
                            saldoInicial = saldoInicial - montoCapital;
                            filaNuevaInt["int_saldo_final"] = (int)saldoInicial;

                            //se agrega la fila al datatable
                            dtInteresPorDev.Rows.Add(filaNuevaInt);
                        }

                    }

                    for (int i3 = 0; i3 < mesesArriendo; i3++)
                    {
                        int cuota = i3 + 1;
                        bool existeCuota = dtIngresosDiferidos.AsEnumerable().Any(row =>
                        row["ing_num_con"].ToString() == dtContratos.Rows[i]["con_num_con"].ToString() &&
                        row["ing_nro_cuota"].ToString() == cuota.ToString());

                        if (!existeCuota)
                        {
                            if (interesDiferido > 0)
                            {
                                DataRow filaNuevaIng = dtIngresosDiferidos.NewRow();
                                filaNuevaIng["ing_num_con"] = dtContratos.Rows[i]["con_num_con"].ToString();
                                filaNuevaIng["ing_nro_cuota"] = cuota;
                                filaNuevaIng["ing_precio_base"] = precioBase;
                                filaNuevaIng["ing_interes_diferido"] = interesDiferido;
                                filaNuevaIng["ing_fecha_contab"] = GetUltimoDiaDelMes(fechaVtoOriginal.AddMonths(cuota - 1));
                                filaNuevaIng["ing_estado_contab"] = 0;
                                dtIngresosDiferidos.Rows.Add(filaNuevaIng);
                            }
                        }
                    }
                }

                correlativo_int_dev++;
                for (int i = 0; i < dtContratos.Rows.Count; i++)
                {
                    if (dtContratos.Rows[i]["con_cuotas_pactadas"] != DBNull.Value)
                    {
                        //se agregan las modificaciones
                        var busqueda = dtModificaciones.Select("numero_contrato='" + dtContratos.Rows[i]["con_num_con"] + "'");  //+ " and tipo_sistema=" + dtContratos.Rows[i]["tipo_sistema"]);
                        foreach (DataRow busquedaModificacion in busqueda)
                        {
                            /*se agrega la modificacion, reactualizacion y/o resciliacion*/
                            DataRow filaInicialMod = dtInteresPorDev.NewRow();
                            filaInicialMod["int_num_con"] = dtContratos.Rows[i]["con_num_con"].ToString();
                            filaInicialMod["int_nro_cuota"] = 0;
                            filaInicialMod["int_correlativo"] = correlativo_int_dev; 

                            if (busquedaModificacion["di18"].ToString() == "1")
                            {
                                filaInicialMod["int_tipo_movimiento"] = "Modificación Inicial";
                            }
                            else if (busquedaModificacion["di18"].ToString() == "2")
                            {
                                filaInicialMod["int_tipo_movimiento"] = "Resciliación";
                            }
                            else if (busquedaModificacion["di18"].ToString() == "3")
                            {
                                filaInicialMod["int_tipo_movimiento"] = "Anulación";
                            }
                            else
                            {
                                filaInicialMod["int_tipo_movimiento"] = "Modificación Inicial";
                            }

                            //si pagó la deuda, dejamos el valor actual 
                            double saldoInicialModInicial = valorActual(Convert.ToInt32(busquedaModificacion["valor_cuota_antiguo"]), Convert.ToInt32(busquedaModificacion["cuotas_pactadas_antiguo"]));
                            filaInicialMod["int_saldo_inicial"] = (int)saldoInicialModInicial;
                            filaInicialMod["int_cuota_final"] = busquedaModificacion["pie_nuevo"];
                            filaInicialMod["int_fecha_vcto"] = busquedaModificacion["fecha_primer_vto"];
                            if (busquedaModificacion["pie_nuevo"] != DBNull.Value)
                            {
                                filaInicialMod["int_saldo_final"] = saldoInicialModInicial - Convert.ToInt32(busquedaModificacion["pie_nuevo"]);
                            }
                            else
                            {
                                filaInicialMod["int_saldo_final"] = saldoInicialModInicial;
                            }
                            filaInicialMod["int_fecha_pago"] = busquedaModificacion["fecha_modificacion"];
                            filaInicialMod["int_cuotas_pactadas_mod"] = busquedaModificacion["cuotas_pactadas_nuevo"];
                            //agrega la fila 
                            dtInteresPorDev.Rows.Add(filaInicialMod);

                            //variables para el ciclo
                            double saldoInicialMod = Convert.ToInt32(busquedaModificacion["total_venta_nuevo"]);
                            double valorCuotaMod = Convert.ToInt32(busquedaModificacion["valor_cuota_nuevo"]);
                            fechaUltPagoCuotaMod = Convert.ToDateTime(busquedaModificacion["fecha_modificacion"]).Date;

                            if (busquedaModificacion["fecha_primer_vto"] != DBNull.Value)
                            {
                                fechaVto = Convert.ToDateTime(busquedaModificacion["fecha_primer_vto"]);
                                fechaVtoOriginal = Convert.ToDateTime(busquedaModificacion["fecha_primer_vto"]);
                            }
                            //si es resciliacion y anulacion no genera registros
                            if ((filaInicialMod["int_tipo_movimiento"].ToString() != "Resciliación" &&
                            filaInicialMod["int_tipo_movimiento"].ToString() != "Anulación"))
                            {
                                for (int i2 = 0; i2 < Convert.ToInt32(busquedaModificacion["cuotas_pactadas_nuevo"]); i2++)
                                {
                                    int numeroCuota = i2 + 1;

                                    DataRow filaNuevaMod = dtInteresPorDev.NewRow();
                                    filaNuevaMod["int_num_con"] = dtContratos.Rows[i]["con_num_con"].ToString();
                                    filaNuevaMod["int_correlativo"] = correlativo_int_dev;
                                    filaNuevaMod["int_nro_cuota"] = numeroCuota;
                                    filaNuevaMod["int_tipo_movimiento"] = "Cuota Modificación";
                                    filaNuevaMod["int_cuotas_pactadas_mod"] = busquedaModificacion["cuotas_pactadas_nuevo"];

                                    if (busquedaModificacion["fecha_primer_vto"] != DBNull.Value)
                                    {
                                        if (numeroCuota == 1)
                                        {
                                            filaNuevaMod["int_fecha_vcto"] = fechaVto.ToShortDateString();
                                            filaNuevaMod["int_fecha_contab"] = GetUltimoDiaDelMes(fechaVto);
                                            filaNuevaMod["int_estado_contab"] = 0;
                                        }
                                        else
                                        {
                                            filaNuevaMod["int_fecha_vcto"] = fechaVtoOriginal.AddMonths(numeroCuota - 1).ToShortDateString();
                                            fechaVto = fechaVtoOriginal.AddMonths(numeroCuota - 1);
                                            filaNuevaMod["int_fecha_contab"] = GetUltimoDiaDelMes(fechaVto);
                                            filaNuevaMod["int_estado_contab"] = 0;
                                        }
                                    }

                                    //vemos si tiene cuota en terreno y se resta para cuadrar
                                    double valorCuotaTerreno = 0;
                                    DataRow[] busquedaPagoCuotaTerreno = dtPagosRealizadosTerreno.Select("fecha_vto='" + fechaVto + "' and contrato='" + dtContratos.Rows[i]["con_num_con"].ToString() + "'");
                                    if (busquedaPagoCuotaTerreno.Length > 0)
                                    {
                                        valorCuotaTerreno = Convert.ToInt32(busquedaPagoCuotaTerreno[0]["valor_cuota"]);
                                    }
                                    //buscamos si la cuota esta pagada
                                    DataRow[] busquedaPagoCuota = dtPagosRealizados.AsEnumerable()
                                                                 .Where(r =>
                                                                     r.Field<DateTime>("fecha_vcto") == fechaVto &&
                                                                     r.Field<string>("contrato") == dtContratos.Rows[i]["con_num_con"].ToString() &&
                                                                     r.Field<int>("valor_cuota") == (valorCuotaMod - valorCuotaTerreno)
                                                                 //r.Field<int>("numero_cuota") == numeroCuota
                                                                 ).ToArray();
                                    if (busquedaPagoCuota.Length > 0)
                                    {
                                        filaNuevaMod["int_fecha_pago"] = busquedaPagoCuota[0]["fecha_pago"];
                                        filaNuevaMod["int_estado_cuota"] = 2; //"Pagado";
                                        fechaUltPagoCuotaMod = (DateTime)busquedaPagoCuota[0]["fecha_pago"];
                                    }
                                    else
                                    {
                                        filaNuevaMod["int_estado_cuota"] = 1; //"Pendiente";
                                    }
                                    //calculo de intereses
                                    filaNuevaMod["int_saldo_inicial"] = (int)saldoInicialMod;
                                    double interes = saldoInicialMod * 0.02;
                                    double montoCapital = valorCuotaMod - interes;
                                    filaNuevaMod["int_tasa_interes"] = (int)interes;
                                    filaNuevaMod["int_cuota_final"] = (int)valorCuotaMod;
                                    filaNuevaMod["int_abono_a_capital"] = (int)montoCapital;
                                    saldoInicialMod = saldoInicialMod - montoCapital;
                                    filaNuevaMod["int_saldo_final"] = (int)saldoInicialMod;

                                    //se agrega la fila al datatable
                                    dtInteresPorDev.Rows.Add(filaNuevaMod);
                                }
                            }
                        }
                    }
                }

                /******busca los datos a borrar*****/
                DataView view = new DataView(dtInteresPorDev);
                view.Sort = "int_num_con asc";
                dtInteresPorDev = view.ToTable();
                //AGREGAMOS UN ID
                int id = 0;
                foreach (DataRow row in dtInteresPorDev.Rows)
                {
                    id = id + 1;
                    row["ID"] = id;
                }
                DataView view2 = new DataView(dtInteresPorDev);
                view2.Sort = "ID asc";
                dtInteresPorDev = view2.ToTable();
                DataTable tblEliminadosID = new DataTable();
                tblEliminadosID.Columns.Add("ID", typeof(Int64));

                var contratosConModificacionInicial = dtInteresPorDev.AsEnumerable()
                    .Where(row => row.Field<string>("int_tipo_movimiento") == "Modicicación Inicial")
                    .Select(row => row.Field<string>("int_num_con"))
                    .Distinct()
                    .ToList();


                foreach (var contrato in contratosConModificacionInicial)
                {
                    var cuotasPendientes = dtInteresPorDev.AsEnumerable()
                        .Where(row => row.Field<string>("int_num_con") == contrato &&
                                      row.Field<int>("int_estado_cuota") == 1 &&
                                      row.Field<string>("int_tipo_movimiento") == "Cuota")
                        .ToList();

                    foreach (var cuota in cuotasPendientes)
                    {
                        DataRow rowElimina = tblEliminadosID.NewRow();
                        rowElimina["ID"] = cuota["ID"];
                        tblEliminadosID.Rows.Add(rowElimina);
                    }
                }

                //for para borrar
                for (int i = 0; i < dtInteresPorDev.Rows.Count; i++)
                {
                    //para eliminar
                    if (dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Modificación Inicial" || dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Anulación"
                        || dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Resciliación")
                    {
                        var busquedaPagosEliminar = dtInteresPorDev.Select("int_estado_cuota =1 and ID<'" + dtInteresPorDev.Rows[i]["ID"] + "' and int_num_con='" + dtInteresPorDev.Rows[i]["int_num_con"] + "'");
                        foreach (DataRow rowEliminar in busquedaPagosEliminar)
                        {
                            DataRow rowElimina = tblEliminadosID.NewRow();
                            rowElimina["ID"] = rowEliminar["ID"];
                            tblEliminadosID.Rows.Add(rowElimina);
                        }
                    }
                }

                // DataTable para guardar los registros a "inactivar"
                DataTable dtInteresPorDevInactivos = dtInteresPorDev.Clone();

                //eliminamos los duplicados del tblEliminados
                if (tblEliminadosID.Rows.Count > 0)
                {
                    var distinctRows = tblEliminadosID.AsEnumerable()
                        .GroupBy(row => new { ID = row["ID"] })
                        .Select(group => group.First())
                        .CopyToDataTable();

                    tblEliminadosID = distinctRows;
                    //ordenamos los eliminados y luego procedemos a borrar
                    DataView orderEliminado = new DataView(tblEliminadosID);
                    orderEliminado.Sort = "ID desc";
                    tblEliminadosID = orderEliminado.ToTable();
                    foreach (DataRow row in tblEliminadosID.Rows)
                    {
                        int index = Convert.ToInt32(row["ID"]) - 1;
                        DataRow rowInactivo = dtInteresPorDev.Rows[index];

                        DataRow copia = dtInteresPorDevInactivos.NewRow();
                        copia.ItemArray = rowInactivo.ItemArray.Clone() as object[];
                        copia["int_estado_cuota"] = 3;
                        dtInteresPorDevInactivos.Rows.Add(copia);

                        dtInteresPorDev.Rows[Convert.ToInt32(row["ID"]) - 1].Delete();
                        //dtInteresPorDev.Rows[Convert.ToInt32(row["ID"]) - 1]["int_estado_cuota"] = 3;
                    }
                }

                correlativo_int_dev++;
                //actualiza los capitales de las modificaciones
                for (int i = 0; i < dtInteresPorDev.Rows.Count; i++)
                {
                    //solo para modificaciones y sus cuotas
                    if (dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Modificación Inicial" &&
                        Convert.ToInt32(dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"].ToString()) > 1)
                    {
                        double saldoInicial = 0;

                        var totalCreditoContrato = dtContratos.AsEnumerable()
                                .Where(row => row.Field<string>("con_num_con") == (string)dtInteresPorDev.Rows[i]["int_num_con"])
                                .Select(row => row.Field<int>("con_total_credito"))
                                .ToList();

                        //si el total del credito es 0 tomamos el total credito del contrato
                        if (Convert.ToInt32(dtInteresPorDev.Rows[i - 1]["int_saldo_final"]) == 0)
                        {
                            if (Convert.ToInt32(totalCreditoContrato) == 0)
                            {
                                saldoInicial = Convert.ToInt32(dtInteresPorDev.Rows[i - 1]["int_saldo_final"]) * Math.Pow((1 + 0.02), mesesAtrasados(dtInteresPorDev.Rows[i - 1]["int_fecha_vcto"].ToString(), dtInteresPorDev.Rows[i]["int_fecha_pago"].ToString()));
                            }
                            else
                            {
                                saldoInicial = Convert.ToInt32(totalCreditoContrato) * Math.Pow((1 + 0.02), mesesAtrasados(dtInteresPorDev.Rows[i - 1]["int_fecha_vcto"].ToString(), dtInteresPorDev.Rows[i]["int_fecha_pago"].ToString()));
                            }
                        }
                        else
                        {
                            saldoInicial = Convert.ToInt32(dtInteresPorDev.Rows[i - 1]["int_saldo_final"]) * Math.Pow((1 + 0.02), mesesAtrasados(dtInteresPorDev.Rows[i - 1]["int_fecha_vcto"].ToString(), dtInteresPorDev.Rows[i]["int_fecha_pago"].ToString()));
                        }
                        dtInteresPorDev.Rows[i]["int_saldo_inicial"] = (int)saldoInicial;
                        dtInteresPorDev.Rows[i]["int_saldo_final"] = (int)saldoInicial - Convert.ToInt32(dtInteresPorDev.Rows[i]["int_cuota_final"]);
                    }

                    //calculo para las cuotas de modificación
                    if (dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Cuota Modificación")
                    {
                        //detalle de las modificaciones
                        double interes = 0;
                        double saldoInicial = 0;
                        saldoInicial = Convert.ToInt32(dtInteresPorDev.Rows[i - 1]["int_saldo_final"]);

                        int valorCuota = Convert.ToInt32(dtInteresPorDev.Rows[i]["int_cuota_final"]);
                        dtInteresPorDev.Rows[i]["int_saldo_inicial"] = (int)saldoInicial;

                        if (dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"] == null || String.IsNullOrEmpty(dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"].ToString()))
                        {
                            dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"] = 0;
                        }

                        if (Convert.ToInt32(dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"]) > 3)
                        {
                            interes = saldoInicial * 0.02;
                        }
                        else
                        {
                            interes = 0;
                        }

                        double montoCapital = valorCuota - interes;
                        dtInteresPorDev.Rows[i]["int_tasa_interes"] = (int)interes;
                        dtInteresPorDev.Rows[i]["int_cuota_final"] = (int)valorCuota;
                        dtInteresPorDev.Rows[i]["int_abono_a_capital"] = (int)montoCapital;
                        saldoInicial = saldoInicial - montoCapital;
                        dtInteresPorDev.Rows[i]["int_saldo_final"] = (int)saldoInicial;
                        dtInteresPorDev.Rows[i]["int_correlativo"] = correlativo_int_dev;
                    }
                }

                //agregamos al datatable dtInteresPorDev las filas con estado 3, anulado por modificación
                if (dtInteresPorDevInactivos.Rows.Count > 0)
                {
                    foreach (DataRow row in dtInteresPorDevInactivos.Rows)
                    {
                        dtInteresPorDev.ImportRow(row);
                    }
                }


                //recorremos el datatable dtInteresPorDev y guardamos en la tabla de la BD
                int contIntDev = 0;
                if (dtInteresPorDev.Rows.Count > 0)
                {
                    foreach (DataRow row in dtInteresPorDev.Rows)
                    {
                        //antes de guardar verificamos si ya existe el registro 
                        bool existeCuota = dtInteresPorDevParaValidar.AsEnumerable().Any(x =>
                        x["int_num_con"].ToString().Trim() == row["int_num_con"].ToString().Trim() &&
                        Convert.ToInt32(x["int_nro_cuota"]) == Convert.ToInt32(row["int_nro_cuota"]));

                        if (!existeCuota)
                        {
                            InteresesPorDevengar intereses = new InteresesPorDevengar
                            {
                                int_num_con = GetStringValue(row, "int_num_con"),
                                int_nro_cuota = GetIntValue(row, "int_nro_cuota"),
                                int_correlativo = GetIntValue(row, "int_correlativo"),
                                int_saldo_inicial = GetIntValue(row, "int_saldo_inicial"),
                                int_tasa_interes = GetIntValue(row, "int_tasa_interes"),
                                int_cuota_final = GetIntValue(row, "int_cuota_final"),
                                int_abono_a_capital = GetIntValue(row, "int_abono_a_capital"),
                                int_saldo_final = GetIntValue(row, "int_saldo_final"),
                                int_estado_cuota = GetIntValue(row, "int_estado_cuota"),
                                int_fecha_pago = GetDateValue(row, "int_fecha_pago"),
                                int_fecha_vcto = GetDateValue(row, "int_fecha_vcto"),
                                int_fecha_contab = GetDateValue(row, "int_fecha_contab"),
                                int_estado_contab = GetIntValue(row, "int_estado_contab"),
                                int_tipo_movimiento = GetStringValue(row, "int_tipo_movimiento"),
                                int_cuotas_pactadas_mod = GetIntValue(row, "int_cuotas_pactadas_mod"), 
                                int_fecha = DateTime.Now
                            };
                            await _connContext.InteresesPorDevengar.AddAsync(intereses);
                        }
                        else { contIntDev++; }
                    }

                    try
                    {
                        //aqui guarda en la BD
                        interesesGuardados = await _connContext.SaveChangesAsync();                        
                    }
                    catch (Exception ex)
                    {
                        await logWriter.WriteLineAsync($"Error al guardar intereses por devengar: {ex.Message} - {DateTime.Now}");
                    }
                }


                //recorremos el datatable dtIngresosDiferidos y guardamos en la tabla de la BD pero primero vemos si ya existe el registro 
                int cont = 0;
                if (dtIngresosDiferidos.Rows.Count > 0) 
                {
                    foreach (DataRow row in dtIngresosDiferidos.Rows)
                    {
                        bool existeCuota = dtIngresosDiferidosParaValidar.AsEnumerable().Any(x =>
                        x["ing_num_con"].ToString().Trim() == row["ing_num_con"].ToString().Trim() &&
                        Convert.ToInt32(x["ing_nro_cuota"]) == Convert.ToInt32(row["ing_nro_cuota"]));

                        if (!existeCuota)
                        {
                            IngresosDiferidos ingresos = new IngresosDiferidos
                            {
                                ing_num_con = GetStringValue(row, "ing_num_con"),
                                ing_precio_base = GetIntValue(row, "ing_precio_base"),
                                ing_nro_cuota = GetIntValue(row, "ing_nro_cuota"),
                                ing_interes_diferido = GetIntValue(row, "ing_interes_diferido"),
                                ing_fecha_contab = GetDateValue(row, "ing_fecha_contab"),
                                ing_estado_contab = GetIntValue(row, "ing_estado_contab"),
                                ing_fecha = DateTime.Now
                            };
                            await _connContext.IngresosDiferidos.AddAsync(ingresos);
                        }
                        else { cont++; }
                    }

                    try
                    {
                        //guarda en la BD
                        ingresosGuardados = await _connContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        await logWriter.WriteLineAsync($"Error al guardar ingresos diferidos: {ex.Message} - {DateTime.Now}");
                    }
                }

                return Ok(new
                {
                    registrosInteresesEsperados = dtInteresPorDev.Rows.Count,
                    registrosInteresesInsertados = interesesGuardados,
                    registrosInteresesYaExisten = 0,
                    registrosIngresosEsperados = dtIngresosDiferidos.Rows.Count,
                    registrosIngresosInsertados = ingresosGuardados,
                    registrosIngresosYaExisten = cont
                });  
            }
        }

        public static DateTime GetUltimoDiaDelMes(DateTime fecha)
        {
            return new DateTime(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));
        }

        public static double valorActual(double valorFuturo, int periodos)
        {
            // Variables
            double tasaInteres = 2.0 / 100;
            int plazo = periodos;
            double monto = valorFuturo;
            double valorActual = 0;

            if (periodos >= 0 && periodos <= 3)
            {
                valorActual = valorFuturo * periodos;
            }
            else
            {
                // Cálculo
                double potencia = Math.Pow(1 + tasaInteres, plazo);
                double resultado = ((potencia - 1) * monto) / (potencia * tasaInteres);

                // Redondeo al múltiplo de 10,000 más cercano y conversión a entero
                /********************CONSIDERAR REDONDEO SOLO CONTRATOS DE 2025 HACIA ATRAS********************/
                double resultadoFinal = (int)redondearExcel(resultado, -4);
                valorActual = resultadoFinal;
                //valorActual = resultado;
            }
            return valorActual;
        }

        public static double mesesAtrasados(string fechaUltVto, string fecha2)
        {
            double retorno = 0;
            if (!String.IsNullOrEmpty(fechaUltVto))
            {
                DateTime fechaUltimoMovimiento = Convert.ToDateTime(fechaUltVto);
                int conteoMeses = 0;
                while (fechaUltimoMovimiento.Date < Convert.ToDateTime(fecha2).Date)
                {
                    fechaUltimoMovimiento = fechaUltimoMovimiento.Date.AddMonths(1);
                    //Console.WriteLine("sumando mes : " + fechaUltimoMovimiento);
                    if (fechaUltimoMovimiento.Date <= Convert.ToDateTime(fecha2).Date)
                    {
                        conteoMeses += 1;
                    }
                }
                fechaUltimoMovimiento = fechaUltimoMovimiento.Date.AddMonths(-1);
                //Console.WriteLine("conteoMeses: " + conteoMeses);
                // SACAMOS LA DIFERENCIA DE LOS DIAS
                TimeSpan ts = Convert.ToDateTime(fecha2).Date - fechaUltimoMovimiento.Date;
                // Difference in days.
                double diferenciaEnDias = ts.Days;
                //Console.WriteLine("fechaUltimoMovimiento: " + fechaUltimoMovimiento);
                //Console.WriteLine("diferenciaEnDias: " + diferenciaEnDias);
                double diasProporcionales = diferenciaEnDias / 30;
                retorno = conteoMeses + Math.Round(diasProporcionales, 2);

                //si es negativo retornamos 0
                if (retorno < 0)
                {
                    retorno = 0;
                }
            }
            return retorno;

        }

        public static double redondearExcel(double value, int digits)
        {
            double pow = Math.Pow(10, digits);
            return Math.Truncate(value * pow + Math.Sign(value) * 0.5) / pow;

        }

        # region "VALIDADORES EN CASO DE NULL O VACIO"
        private static int GetIntValue(DataRow row, string columnName)
        {
            return row[columnName] == DBNull.Value || string.IsNullOrWhiteSpace(row[columnName].ToString()) ? 0 : Convert.ToInt32(row[columnName]);
        }

        private static string GetStringValue(DataRow row, string columnName)
        {
            return row[columnName] == DBNull.Value ? string.Empty : row[columnName].ToString();
        }

        private static DateTime? GetDateValue(DataRow row, string columnName)
        {
            return row[columnName] == DBNull.Value || string.IsNullOrWhiteSpace(row[columnName].ToString()) ? (DateTime?)null : Convert.ToDateTime(row[columnName]);
        }
        #endregion
    }
}
