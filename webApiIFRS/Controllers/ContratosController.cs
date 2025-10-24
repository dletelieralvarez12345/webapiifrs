using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualBasic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using System.Transactions;
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
        public static int _interesesGuardados = 0;
        public static int _ingresosGuardados = 0;
        public static int _interesesDevExistentes = 0;
        public static int _ingresosDifNichosExistentes = 0;
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
                 .FirstOrDefaultAsync(x => x.con_num_con == con_num_con);
            // .FirstOrDefaultAsync(x => x.con_id == id && x.con_num_con == con_num_con); 

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
        public async Task<ActionResult<IngresosDiferidosNichos>> GetIngresosDiferidosByContrato(string ing_num_con)
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
            DataTable dtContratosOriginal = new DataTable();
            DataTable dtPagosRealizados = new DataTable();
            DataTable dtPagosRealizadosTerreno = new DataTable();
            DataTable dtModificaciones = new DataTable();
            DataTable dtFechaPrimerVto = new DataTable();
            DataTable dtInteresPorDev = new DataTable();
            DataTable dtIngresosDiferidos = new DataTable();
            DataTable dtInteresPorDevParaValidar = new DataTable();
            DataTable dtIngresosDiferidosParaValidar = new DataTable();
            DataTable dtInteresPorDevInactivos = new DataTable();
            DataTable dtDerechosServicios = new DataTable();

            DateTime fechaVto = new DateTime();
            DateTime fechaVtoOriginal = new DateTime();
            DateTime fechaUltPagoCuota = new DateTime();
            DateTime fechaUltPagoCuotaMod = new DateTime();
            DateTime fechaIngresoContrato = new DateTime(); 

            var interesesPorDevengar = new List<InteresesPorDevengar>();
            var ingresosDiferidos = new List<IngresosDiferidosNichos>();
            double tasaInteres = 2.0 / 100;

            /*OBTENER TODOS LOS CONTRATOS*/
            dtContratos = await _connContext.ListaIngresosDeVentasAllContratos(); 
            dtContratosOriginal = await _connContext.ListaIngresosDeVentasAllContratos(); 
            /*CONTRATOS QUE SE DUPLICAN EN dtContratos*/
            var contratosDuplicados = dtContratos.AsEnumerable()
                .GroupBy(r => r.Field<string>("con_num_con"))
                .Where(gr => gr.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet();
            /*DATATABLE SIN LOS DUPLICADOS*/
            var dtSinDuplicados = dtContratos.AsEnumerable()
                .Where(r => !contratosDuplicados.Contains(r.Field<string>("con_num_con")))
                .CopyToDataTable();
            /*REEMPLAZA EL DT ORIGINAL*/
            dtContratos = dtContratos.AsEnumerable()
                .Where(r => !contratosDuplicados.Contains(r.Field<string>("con_num_con")))
                .CopyToDataTable();

            dtDerechosServicios = await _connContext.ObtenerDerechosServiciosSinIva(2025); 

            var derechosServiciosPorContrato = dtDerechosServicios
                .AsEnumerable()
                .GroupBy(r => r.Field<int>("numero_comprobante"))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r => Convert.ToDecimal(r["total_serv_der_con_iva"]))
                );

            if (!dtContratos.Columns.Contains("con_derechos_servicios_con_iva"))
            {
                dtContratos.Columns.Add("con_derechos_servicios_con_iva", typeof(decimal));
            }

            foreach (DataRow row in dtContratos.Rows)
            {
                var contrato = row.Field<int>("con_num_comprobante");
                if (contrato != 0 &&
                    derechosServiciosPorContrato.TryGetValue(contrato, out decimal total))
                {
                    row["con_derechos_servicios_con_iva"] = total;
                }
                else
                {
                    row["con_derechos_servicios_con_iva"] = 0m; 
                }
            }

            /*RECORRO DTCONTRATO PARA REALIZAR LOS CALCULOS*/
            foreach (DataRow row in dtContratos.Rows)
            {
                string numContrato = row.Field<string>("con_num_con");
                int numComprobante = row.Field<int>("con_num_comprobante");
                decimal pie = row.Field<decimal>("con_pie");
                int cuotas = row.Field<int>("con_cuotas_pactadas");
                decimal valorCuota = row.Field<decimal>("con_valor_cuota_pactada");
                decimal precioBase = row.Field<decimal>("con_precio_base");     
                decimal totalCredito = row.Field<decimal>("con_total_credito");
                int tipoIngreso = row.Field<int>("con_id_tipo_ingreso");

                // derechos por contrato (si no hay, 0m)
                decimal totalDerechos = 0m;
                if (numComprobante != 0 && derechosServiciosPorContrato.TryGetValue(numComprobante, out var td))
                {
                    totalDerechos = td;
                }

                // total venta 
                decimal totalVenta = pie + (cuotas * valorCuota);
                decimal precioBaseMenosDerechos = 0;

                // si es Ingreso de Nichos (1), cuotas=0 y totalCredito=0 => contado: suma derechos
                if (tipoIngreso == 1 && numContrato == numComprobante.ToString() && cuotas == 0 && totalCredito == 0)
                {
                    totalVenta += totalDerechos;
                    pie = totalVenta;
                    precioBaseMenosDerechos = totalVenta - totalDerechos;
                }
                else
                {
                    // precio base neto de derechos (si precioBase original incluye derechos)
                    precioBaseMenosDerechos = precioBase - totalDerechos;
                }

                // setea resultados en la misma fila
                row["con_derechos_servicios_con_iva"] = totalDerechos;
                row["con_total_venta"] = totalVenta;
                row["con_precio_base"] = precioBaseMenosDerechos;
                row["con_pie"] = pie; 
            }


            /*OBTENGO TODA LA INFORMACION MENOS TOTAL_VENTA PARA LOS CONTRATOS DUPLICADOS Y SE AGREGAN LOS CONTRATOS QUE ESTABAN DUPLICADOS AHORA CALCULANDO SU TOTALVENTA*/
            foreach (var con in contratosDuplicados)
            {
                var busqueda = dtContratosOriginal.AsEnumerable()
                    .Where(row => row.Field<string>("con_num_con") == con)
                    .FirstOrDefault();
        
                DataRow filaNew = dtContratos.NewRow();
                string numeroComprobante = busqueda[2].ToString(); 
                decimal pie = decimal.Parse(busqueda[6].ToString()); 
                int cuotasPactadas = int.Parse(busqueda[9].ToString()); 
                decimal valorCuota = decimal.Parse(busqueda[10].ToString());
                decimal precioBase = decimal.Parse(busqueda[5].ToString());
                decimal totalDerechos = 0;
                decimal totalCredito = decimal.Parse(busqueda[7].ToString());
                int tipoIngreso = int.Parse(busqueda[2].ToString());
                decimal totalVenta = pie + (cuotasPactadas * valorCuota); 

                if (derechosServiciosPorContrato.TryGetValue(int.Parse(busqueda[1].ToString()), out totalDerechos))
                {
                    filaNew["con_derechos_servicios_con_iva"] = totalDerechos;
                }
                else
                {
                    filaNew["con_derechos_servicios_con_iva"] = 0m;
                }

                //si tipo de ingreso es Ingreso de Nichos y cuotas es igual a 0 y total credito = 0 es pago al CONTADO. 
                if (tipoIngreso == 1 && con == numeroComprobante &&  cuotasPactadas == 0 && totalCredito == 0)
                {
                    totalVenta += totalDerechos;
                    pie = totalVenta;
                }

                decimal precioBaseMenosDerSerConIva = precioBase - totalDerechos; 
                filaNew["con_num_con"] = busqueda[0].ToString();
                filaNew["con_num_comprobante"] = busqueda[1].ToString();
                filaNew["con_id_tipo_ingreso"] = tipoIngreso; 
                filaNew["con_fecha_ingreso"] = busqueda[3].ToString();
                filaNew["con_total_venta"] = totalVenta;
                filaNew["con_precio_base"] = precioBaseMenosDerSerConIva;
                filaNew["con_pie"] = pie;
                filaNew["con_total_credito"] = totalCredito;
                filaNew["con_cuotas_pactadas"] = cuotasPactadas;
                filaNew["con_valor_cuota_pactada"] = valorCuota;
                filaNew["con_tasa_interes"] = busqueda[11].ToString();
                filaNew["con_capacidad_sepultura"] = busqueda[12].ToString();
                filaNew["con_tipo_compra"] = busqueda[13].ToString();
                filaNew["con_terminos_pago"] = busqueda[14].ToString();
                filaNew["con_nombre_cajero"] = busqueda[15].ToString();
                filaNew["con_fecha_primer_vcto_ori"] = busqueda[16].ToString();
                filaNew["con_tipo_movimiento"] = busqueda[17].ToString();
                filaNew["con_cuotas_pactadas_mod"] = busqueda[18].ToString();
                filaNew["con_estado_contrato"] = busqueda[19].ToString();
                filaNew["con_num_repactaciones"] = busqueda[20].ToString();
                filaNew["con_anos_arriendo"] = busqueda[21].ToString();
                dtContratos.Rows.Add(filaNew);                
            }

            /*YA CON EL DATATABLE LIMPIO SIN DUPLICADOS Y CON EL MONTO TOTAL VENTA, PROCEDO A INSERTARLOS A LA BD*/            
            string logPathContratos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_CargaContratos.txt");
            int contratosGuardados = 0;
            int contContratosExistentes = 0;

            using (StreamWriter logWriterContratos = new StreamWriter(logPathContratos, append: true))
            {                
                if (dtContratos.Rows.Count > 0)
                {
                    foreach (DataRow row in dtContratos.Rows)
                    {
                        var numCon = row["con_num_con"].ToString().Trim();

                        bool existeContratoIngresado = await _connContext.Contrato
                            .AnyAsync(c => c.con_num_con == numCon); 
                        
                            try
                            {
                                Contrato contrato = new Contrato
                                {
                                    con_num_con = GetStringValue(row, "con_num_con"),
                                    con_num_comprobante = GetIntValue(row,"con_num_comprobante"), 
                                    con_id_tipo_ingreso = GetIntValue(row, "con_id_tipo_ingreso"),
                                    con_fecha_ingreso = GetDateValue(row, "con_fecha_ingreso"),
                                    con_total_venta = GetDecimalValue(row, "con_total_venta"),
                                    con_precio_base = GetDecimalValue(row, "con_precio_base"),
                                    con_pie = GetDecimalValue(row, "con_pie"),
                                    con_total_credito = GetDecimalValue(row, "con_total_credito"),
                                    con_cuotas_pactadas = GetIntValue(row, "con_cuotas_pactadas"),
                                    con_valor_cuota_pactada = GetDecimalValue(row, "con_valor_cuota_pactada"),
                                    con_tasa_interes = GetIntValue(row, "con_tasa_interes"),
                                    con_capacidad_sepultura = GetIntValue(row, "con_capacidad_sepultura"),
                                    con_tipo_compra = GetStringValue(row, "con_tipo_compra"),
                                    con_terminos_pago = GetStringValue(row, "con_terminos_pago"),
                                    con_nombre_cajero = GetStringValue(row, "con_nombre_cajero"),
                                    con_fecha_primer_vcto_ori = GetDateValue(row, "con_fecha_primer_vcto_ori"),
                                    con_tipo_movimiento = GetIntValue(row, "con_tipo_movimiento"),
                                    con_cuotas_pactadas_mod = GetIntValue(row, "con_cuotas_pactadas_mod"),
                                    con_estado_contrato = GetStringValue(row, "con_estado_contrato"),
                                    con_num_repactaciones = GetIntValue(row, "con_num_repactaciones"),
                                    con_anos_arriendo = GetIntValue(row, "con_anos_arriendo"),
                                    con_derechos_servicios_con_iva = GetDecimalValue(row, "con_derechos_servicios_con_iva")
                                };

                                if (!existeContratoIngresado)
                                {
                                    await _connContext.Contrato.AddAsync(contrato);
                                }
                                else {
                                    contContratosExistentes++; 
                                    await logWriterContratos.WriteLineAsync($"Contrato {contrato.con_num_con} ya existe. Omitido");
                                }
                            }
                            catch (Exception ex)
                            {
                                await logWriterContratos.WriteLineAsync($"Error al preparar datos de contratos (numContrato: {GetStringValue(row, "con_num_con")}) - Excepcion: {ex.Message} - {DateTime.Now}");
                            }
                    }
                }

                try
                {
                    contratosGuardados = await _connContext.SaveChangesAsync();
                    dtContratos = await _connContext.ListaContratosPorAnio(2025); //.ListaIngresosDeVentasAllContratos();
                }
                catch (Exception ex)
                {
                    await logWriterContratos.WriteLineAsync($"Error al ingresar los contratos a la base de datos - Excepcion: {ex.Message} - {DateTime.Now}");
                }
            }

            /*************************************************************************************************************************/            

            /*VERIFICAR EL ESTADO DE LAS CUOTAS*/
            dtPagosRealizados = await _connContext.ObtenerPagosRealizados(2025);
            dtPagosRealizadosTerreno = await _connContext.ObtenerPagosRealizadosTerreno(2025);
            dtModificaciones = await _connContext.ObtenerModificaciones(2025);
            dtFechaPrimerVto = await _connContext.ObtenerFechaPrimerVctoBov(2025);
            dtInteresPorDevParaValidar = await _connContext.ObtenerInteresPorDev_ListadoContratosYsusCuotas(2025);
            dtIngresosDiferidosParaValidar = await _connContext.ObtenerIngresosDiferidosNichos_ListaCuotas(2025);

            int correlativo_int_dev = 0; 

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_0_ProcesaContratos.txt");

            using (StreamWriter logWriter = new StreamWriter(logPath, append: true))
            {
                for (int i = 0; i < dtContratos.Rows.Count; i++)
                {
                    correlativo_int_dev = 0;

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
                    double saldoInicial = Convert.ToDouble(dtContratos.Rows[i]["con_total_credito"]);
                    double valorCuota = Convert.ToDouble(dtContratos.Rows[i]["con_valor_cuota_pactada"]);
                    fechaUltPagoCuota = Convert.ToDateTime(dtContratos.Rows[i]["con_fecha_ingreso"]).Date;
                    fechaIngresoContrato = Convert.ToDateTime(dtContratos.Rows[i]["con_fecha_ingreso"]).Date;

                    //intereses diferidos
                    decimal interesDiferido = 0;
                    int mesesArriendo = 0;
                    decimal totalCredito = decimal.Parse(dtContratos.Rows[i]["con_total_credito"].ToString());
                    decimal pie = decimal.Parse(dtContratos.Rows[i]["con_pie"].ToString());
                    decimal derechos_servicios = decimal.Parse(dtContratos.Rows[i]["con_derechos_servicios_con_iva"].ToString());
                    decimal calculoIngresosADiferir = totalCredito + pie - derechos_servicios;
                    decimal precioBase = decimal.Parse(dtContratos.Rows[i]["con_precio_base"].ToString());

                    /*****TEMPORALIDAD NICHOS****/
                       /*
                        codigo 1 = 5 años
                        2 = 10 años
                        3 = perpetuo
                        4 = 2 años
                        5 = 1 año
                        6 = doble perpetuo 
                        7 = 50 años
                        8 = 3 años                        
                        */
                    /****************************/

                    if (int.Parse(dtContratos.Rows[i]["con_id_tipo_ingreso"].ToString()) == 1 && int.Parse(dtContratos.Rows[i]["con_anos_arriendo"].ToString()) > 0)
                    {
                        mesesArriendo = Convert.ToInt32(dtContratos.Rows[i]["con_anos_arriendo"]) * 12;                        
                        interesDiferido = calculoIngresosADiferir / mesesArriendo; 
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
                        dtIngresosDiferidos.Columns.Add("ing_precio_base", typeof(decimal));
                        dtIngresosDiferidos.Columns.Add("ing_a_diferir", typeof(decimal));
                        dtIngresosDiferidos.Columns.Add("ing_nro_cuota", typeof(int));
                        dtIngresosDiferidos.Columns.Add("ing_interes_diferido", typeof(decimal));
                        dtIngresosDiferidos.Columns.Add("ing_fecha_devengo", typeof(DateTime)); 
                        dtIngresosDiferidos.Columns.Add("ing_fecha_contab", typeof(DateTime));
                        dtIngresosDiferidos.Columns.Add("ing_estado_contab", typeof(int));
                    }

                    correlativo_int_dev++;

                    /*TOMO SOLO LOS CONTRATOS CON CUOTAS MAYOR A CERO PARA GUARDAR LOS INTERESES POR DEVENGAR*/
                    DataTable dtContratosEnCuotas = new DataTable();
                    dtContratosEnCuotas = dtContratos.AsEnumerable()
                        .Where(row => row.Field<int>("con_cuotas_pactadas") > 0)
                        .CopyToDataTable();

                    string logPath_paso1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_1_cargaDTInteresesPorDevengar.txt");

                    using (StreamWriter logWriter_paso1 = new StreamWriter(logPath_paso1, append: true))
                    {                        
                        for (int i2 = 0; i2 < Convert.ToInt32(dtContratos.Rows[i]["con_cuotas_pactadas"].ToString()); i2++)
                        {
                            int numeroCuota = i2 + 1;
                            try
                            {
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
                            catch (Exception ex)
                            {
                                logWriter_paso1.WriteLineAsync($"Falló --> Cuota n°: {numeroCuota} , Contrato: {dtContratos.Rows[i]["con_num_con"].ToString()},  Excepcion capturada: {ex.Message} - {DateTime.Now}");
                            }
                            finally
                            {
                                if (dtInteresPorDev.Rows.Count == 0)
                                {
                                    logWriter_paso1.WriteLineAsync($"Datatable dtInteresPorDev tiene 0 registros - {DateTime.Now}");
                                }                                
                            }
                        }
                    }

                    /*INGRESOS DIFERIDOS DE CONTRATOS ARRIENDO NICHOS*/
                    int tipoIngreso = int.Parse(dtContratos.Rows[i]["con_id_tipo_ingreso"].ToString()); 
                    if (tipoIngreso == 1)
                    {
                        string numeroContrato = dtContratos.Rows[i]["con_num_con"].ToString();
                        string logPath_paso2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_2_cargaDTMesesArriendo.txt");
                        using (StreamWriter logWriter_paso2 = new StreamWriter(logPath_paso2, append: true))
                        {
                            for (int i3 = 0; i3 < mesesArriendo; i3++)
                            {
                                int cuota = i3 + 1;
                                DateTime fechaIngreso_ = fechaIngresoContrato.Date.AddMonths(cuota - 1); //fechaVtoOriginal.AddMonths(cuota - 1);
                                DateTime fechaDevengo = fechaIngreso_.AddMonths(1);

                                try
                                {
                                    bool existeCuota = dtIngresosDiferidos.AsEnumerable().Any(row =>
                                    row["ing_num_con"].ToString() == numeroContrato &&
                                    row["ing_nro_cuota"].ToString() == cuota.ToString());

                                    if (!existeCuota)
                                    {
                                        if (interesDiferido > 0)
                                        {
                                            DataRow filaNuevaIng = dtIngresosDiferidos.NewRow();
                                            filaNuevaIng["ing_num_con"] = numeroContrato;
                                            filaNuevaIng["ing_nro_cuota"] = cuota;
                                            filaNuevaIng["ing_precio_base"] = precioBase;
                                            filaNuevaIng["ing_a_diferir"] = calculoIngresosADiferir;
                                            filaNuevaIng["ing_interes_diferido"] = interesDiferido;
                                            filaNuevaIng["ing_fecha_devengo"] = fechaDevengo;
                                            filaNuevaIng["ing_fecha_contab"] = DBNull.Value;  // GetUltimoDiaDelMes(fechaVtoOriginal.AddMonths(cuota - 1));
                                            filaNuevaIng["ing_estado_contab"] = 0;
                                            dtIngresosDiferidos.Rows.Add(filaNuevaIng);
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    logWriter_paso2.WriteLineAsync($"Falló for meses de arriendo, contrato: {numeroContrato}, cuota : {cuota} Excepcion capturada: {ex.Message} - {DateTime.Now}");
                                }
                                finally
                                {
                                    if (dtIngresosDiferidos.Rows.Count == 0)
                                    {
                                        logWriter.WriteLineAsync($"DataTable dtIngresosDiferidos no tiene registros - {DateTime.Now}");
                                    }
                                }
                            }
                        }
                    }
                }

                correlativo_int_dev++;

                string logPathPaso3 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_3_cargaDTModReacResc.txt");
                using (StreamWriter logWriterPaso3 = new StreamWriter(logPathPaso3, append: true))
                {
                    for (int i = 0; i < dtContratos.Rows.Count; i++)
                    {
                        try
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
                        catch (Exception ex)
                        {
                            logWriterPaso3.WriteLineAsync($"Proceso que verifica modificaciones, resciliaciones o anulaciones presenta un problema en el contrato n°: {dtContratos.Rows[i]["con_num_con"].ToString()} - {DateTime.Now}");
                        }
                    }
                }

                /******busca los datos a borrar*****/
                string logPathPaso4 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_4_Elimina_e_Inactiva.txt");
                using (StreamWriter logWriterPaso4 = new StreamWriter(logPathPaso4, append: true))
                {
                    try
                    {
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
                            if (dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Modificación Inicial" 
                                || dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Anulación"
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
                        dtInteresPorDevInactivos = dtInteresPorDev.Clone();

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
                    }
                    catch (Exception ex)
                    {
                        logWriterPaso4.WriteLineAsync($"Paso 4, Elimina e Inactivo, Excepcion: {ex.Message} - {DateTime.Now}");
                    }
                }

                //correlativo_int_dev++;
                //actualiza los capitales de las modificaciones
                string logPathPaso5 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_5_ActualizaLosCapitalesDeLasModificaciones.txt");
                using (StreamWriter logWriterPaso5 = new StreamWriter(logPathPaso5, append: true))
                {
                    try
                    {
                        for (int i = 0; i < dtInteresPorDev.Rows.Count; i++)
                        {
                            //solo para modificaciones y sus cuotas
                            if (dtInteresPorDev.Rows[i]["int_tipo_movimiento"].ToString() == "Modificación Inicial" &&
                                Convert.ToInt32(dtInteresPorDev.Rows[i]["int_cuotas_pactadas_mod"].ToString()) > 1)
                            {
                                double saldoInicial = 0;

                                var totalCreditoContrato = dtContratos.AsEnumerable()
                                        .Where(row => row.Field<string>("con_num_con") == (string)dtInteresPorDev.Rows[i]["int_num_con"] && row.Field<int>("con_cuotas_pactadas") > 0)
                                        .Select(row => row.Field<decimal>("con_total_credito"))
                                        .FirstOrDefault();
                                
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
                    }
                    catch (Exception ex)
                    {
                        logWriterPaso5.WriteLineAsync($"Paso 5, actualiza los capitales de las modificaciones, Excepcion: {ex.Message} - {DateTime.Now}");
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
                    dtInteresPorDev = await Paso6_GuardaEnBDInteresesPorDevengar(_connContext, dtInteresPorDev, contIntDev);
                }

                //recorremos el datatable dtIngresosDiferidos y guardamos en la tabla de la BD pero primero vemos si ya existe el registro 
                int cont = 0;
                if (dtIngresosDiferidos.Rows.Count > 0) 
                {
                    cont = await Paso7_GuardaEnBDIngresosDiferidos(_connContext, dtIngresosDiferidos, dtIngresosDiferidosParaValidar); 
                }

                return Ok(new
                {
                    registrosContratosEsperados = dtContratos.Rows.Count,
                    registrosContratosGuardados = contratosGuardados,
                    registrosContratosExistentes = contContratosExistentes,

                    registrosInteresesEsperados = dtInteresPorDev.Rows.Count,
                    registrosInteresesInsertados = _interesesGuardados,
                    registrosInteresesYaExisten = _interesesDevExistentes,

                    registrosIngresosEsperados = dtIngresosDiferidos.Rows.Count,
                    registrosIngresosInsertados = _ingresosGuardados,
                    registrosIngresosYaExisten = _ingresosDifNichosExistentes
                });    
            }
        }

        public async static Task<DataTable> Paso6_GuardaEnBDInteresesPorDevengar(ConnContext _connContext, DataTable dtInteresPorDev, int contIntDev)
        {
            DataTable dt = new DataTable();
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_6_GuardaEnBDInteresesPorDevengar.txt");

            using (StreamWriter logWriterPaso6 = new StreamWriter(logPath, append: true))
            {
                using var transaction = await _connContext.Database.BeginTransactionAsync();

                    var clavesExistentes = await _connContext.InteresesPorDevengar
                        .Select(x => new { x.int_num_con, x.int_nro_cuota })
                        .ToListAsync();

                    //Crea un HashSet con claves únicas
                    var hashClaves = new HashSet<(string, int)>(
                    clavesExistentes.Select(c => (c.int_num_con.Trim(), c.int_nro_cuota)));

                    int registrosInsertados = 0;
                    int registrosDuplicados = 0;

                    foreach (DataRow row in dtInteresPorDev.Rows)
                    {
                        string numCon = GetStringValue(row, "int_num_con").Trim();
                        int nroCuota = GetIntValue(row, "int_nro_cuota");

                        try
                        {
                            bool existeEnBD = await _connContext.InteresesPorDevengar
                                             .AnyAsync(x => x.int_num_con == numCon && x.int_nro_cuota == nroCuota);

                            if (!existeEnBD)
                            {
                                var intereses = new InteresesPorDevengar
                                {
                                    int_num_con = numCon,
                                    int_nro_cuota = nroCuota,
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

                                // Opcional: Agregar la nueva clave al HashSet para evitar duplicados en el mismo lote
                                hashClaves.Add((numCon, nroCuota));
                                registrosInsertados++;
                            }
                            else
                            {
                                registrosDuplicados++;
                                _interesesDevExistentes = registrosDuplicados; 
                                await logWriterPaso6.WriteLineAsync($"Registro duplicado en BD (numContrato: {numCon}, numCuota: {nroCuota}). Omitido - {DateTime.Now}");
                            }
                        }
                        catch (Exception ex)
                        {
                            //revierte en caso de error
                            if (transaction.GetDbTransaction().Connection != null)
                            {
                                await transaction.RollbackAsync();
                            }
                            await logWriterPaso6.WriteLineAsync($"Error al preparar datos de intereses por devengar (numContrato: {numCon}, numCuota: {nroCuota}) - Excepcion: {ex.Message} - {DateTime.Now}");
                        }
                    }

                    try
                    {
                        //aqui guarda en la BD                        
                        await _connContext.SaveChangesAsync();
                        await _connContext.ActualizarInteresesPorDevengarSegunContrato();
                        await transaction.CommitAsync(); //confirma todo   
                        _interesesGuardados = registrosInsertados;

                        await logWriterPaso6.WriteLineAsync(
                            $"intereses por devengar insertados: {registrosInsertados}, duplicados omitidos: {registrosDuplicados} - {DateTime.Now}"
                        );
                    }
                    catch (Exception ex)
                    {
                        await logWriterPaso6.WriteLineAsync($"Paso 6, Excepcion: {ex.Message} - {DateTime.Now}");
                    }

                    dt = dtInteresPorDev;
                return dt;
            }
        }

        public async static Task<Int32> Paso7_GuardaEnBDIngresosDiferidos(ConnContext _connContext, DataTable dtIngresosDiferidos, DataTable dtIngresosDiferidosParaValidar)
        {
            int countIngresosDif = 0;
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now.ToShortDateString()}_7_GuardaEnBDIngresosDiferidos.txt");

            using (StreamWriter logWriterPaso7 = new StreamWriter(logPath, append: true))
            {
                int registrosInsertados = 0;
                int registrosDuplicados = 0;

                var clavesExistentes = await _connContext.IngresosDiferidos
                                        .Select(x => new { x.ing_num_con, x.ing_nro_cuota })
                                        .ToListAsync();

                var hashClaves = new HashSet<(string, int)>(
                                    clavesExistentes.Select(c => (c.ing_num_con.Trim(), c.ing_nro_cuota))
                                );

                foreach (DataRow row in dtIngresosDiferidos.Rows)
                {
                    string numCon = GetStringValue(row, "ing_num_con").Trim();
                    int nroCuota = GetIntValue(row, "ing_nro_cuota");

                    bool yaExiste = hashClaves.Contains((numCon, nroCuota));

                    if (!yaExiste)
                    {
                        try
                        {
                            var ingresos = new IngresosDiferidosNichos
                            {
                                ing_num_con = GetStringValue(row, "ing_num_con"),
                                ing_precio_base = GetDecimalValue(row, "ing_precio_base"),
                                ing_a_diferir = GetDecimalValue(row, "ing_a_diferir"),
                                ing_nro_cuota = GetIntValue(row, "ing_nro_cuota"),
                                ing_interes_diferido = GetDecimalValue(row, "ing_interes_diferido"),
                                ing_fecha_devengo = GetDateValue(row, "ing_fecha_devengo"),
                                ing_fecha_contab = GetDateValue(row, "ing_fecha_contab"),
                                ing_estado_contab = GetIntValue(row, "ing_estado_contab"),
                                ing_fecha = DateTime.Now
                            };
                            await _connContext.IngresosDiferidos.AddAsync(ingresos);
                            registrosInsertados++;
                            hashClaves.Add((numCon, nroCuota));
                        }
                        catch (Exception ex)
                        {
                            await logWriterPaso7.WriteLineAsync($"Error al preparar datos de ingresos diferidos (numContrato: {GetStringValue(row, "ing_num_con")}, numCuota: {GetIntValue(row, "ing_nro_cuota")}) - Excepcion: {ex.Message} - {DateTime.Now}");
                        }
                    }
                    else
                    {
                        registrosDuplicados++;
                        _ingresosDifNichosExistentes = registrosDuplicados;
                        await logWriterPaso7.WriteLineAsync(
                            $"Registro duplicado omitido en Ingresos Diferidos Nichos (numContrato: {numCon}, numCuota: {nroCuota}) - {DateTime.Now}"
                        );
                    }
                }

                try
                {
                    //guarda en la BD
                    await _connContext.SaveChangesAsync();
                    _ingresosGuardados = registrosInsertados;

                    await logWriterPaso7.WriteLineAsync(
                        $"Ingresos diferidos insertados: {registrosInsertados}, duplicados omitidos: {registrosDuplicados} - {DateTime.Now}"
                    );
                }
                catch (Exception ex)
                {
                    await logWriterPaso7.WriteLineAsync($"Paso 7, Excepcion: {ex.Message} - {DateTime.Now}");
                }

                countIngresosDif = _ingresosGuardados; 
                return countIngresosDif; 
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

        private static decimal GetDecimalValue(DataRow row, string columnName)
        {
            if (row == null || !row.Table.Columns.Contains(columnName)) return 0m;
            return row.Field<decimal?>(columnName) ?? 0m;
        }
        #endregion
    }
}
