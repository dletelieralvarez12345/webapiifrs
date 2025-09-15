using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using webApiIFRS.Models;

namespace webApiIFRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContratosController : ControllerBase
    {
        //inyección del contexto de la BD 
        private readonly ConnContext _connContext;
        public ContratosController(ConnContext connContext)
        {
            _connContext = connContext;
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
        public async Task<ActionResult<Intereses_Por_Devengar>> GetInteresesPorDevengarDeUnContrato(string int_num_con)
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
        public async Task<ActionResult<Ingresos_Diferidos>> GetIngresosDiferidosByContrato(string ing_num_con)
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
            if(_connContext.Contrato == null)
            {
                return NotFound("No hay contratos que procesar");
            }

            //obtener todos los contratos
            var contratos = await _connContext.Contrato.ToListAsync();
            var interesesPorDevengar = new List<Intereses_Por_Devengar>(); 
            var ingresosDiferidos = new List<Ingresos_Diferidos>();
            double tasaInteres = 2.0 / 100;
            int interesDiferido = 0; 

            foreach (var item in contratos)
            {
                decimal saldoInicial = item.con_precio_base - item.con_pie;
                DateTime fechaPrimerVcto = item.con_fecha_primer_vcto_ori;
                int mesesArriendo = 0; 
                if (item.con_id_tipo_ingreso == 1 && item.con_anos_arriendo > 0)
                {
                    mesesArriendo = item.con_anos_arriendo * 12;
                    interesDiferido = item.con_precio_base / mesesArriendo; 
                }

                for (int i= 0; i < item.con_cuotas_pactadas; i++)
                {
                    decimal interes = saldoInicial * (decimal)tasaInteres;
                    int cuota = item.con_valor_cuota_pactada;
                    decimal abonoCapital = cuota - interes;
                    decimal saldoFinal = saldoInicial - abonoCapital; 

                    interesesPorDevengar.Add(new Intereses_Por_Devengar
                    {
                        int_num_con = item.con_num_con,
                        int_nro_cuota = i+1,
                        int_saldo_inicial = (int)saldoInicial,
                        int_tasa_interes = (int)interes, 
                        int_cuota_final = cuota,
                        int_abono_a_capital = (int)abonoCapital,
                        int_saldo_final = (int)saldoFinal,
                        int_fecha_vcto = fechaPrimerVcto,
                        int_fecha_contab = GetUltimoDiaDelMes(fechaPrimerVcto),
                        int_estado_contab = 0
                    });

                    saldoInicial = saldoFinal;
                    fechaPrimerVcto = fechaPrimerVcto.AddMonths(1); 
                }

                if (mesesArriendo > 0)
                {
                    DateTime fecha_PrimerVcto = item.con_fecha_primer_vcto_ori;
                    for (int i = 0; i < mesesArriendo; i++)
                    {
                        ingresosDiferidos.Add(new Ingresos_Diferidos
                        {
                            ing_num_con = item.con_num_con,
                            ing_precio_base = item.con_precio_base,
                            ing_nro_cuota = i + 1,
                            ing_interes_diferido = interesDiferido,
                            ing_fecha_contab = GetUltimoDiaDelMes(fecha_PrimerVcto),                            
                            ing_estado_contab = 0
                        });

                        fecha_PrimerVcto = fecha_PrimerVcto.AddMonths(1);
                    }
                }
            }

            await _connContext.InteresesPorDevengar.AddRangeAsync(interesesPorDevengar);
            await _connContext.IngresosDiferidos.AddRangeAsync(ingresosDiferidos);
            await _connContext.SaveChangesAsync();

            return Ok(new { ContratosProcesados = contratos.Count }); 

        }

        public static DateTime GetUltimoDiaDelMes(DateTime fecha)
        {
            return new DateTime(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));
        }
    }
}
