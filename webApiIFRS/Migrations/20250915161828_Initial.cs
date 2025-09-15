using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webApiIFRS.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contrato",
                columns: table => new
                {
                    con_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    con_num_con = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    con_id_tipo_ingreso = table.Column<int>(type: "int", nullable: false),
                    con_fecha_ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    con_total_venta = table.Column<int>(type: "int", nullable: false),
                    con_precio_base = table.Column<int>(type: "int", nullable: false),
                    con_pie = table.Column<int>(type: "int", nullable: false),
                    con_total_credito = table.Column<int>(type: "int", nullable: false),
                    con_cuotas_pactadas = table.Column<int>(type: "int", nullable: false),
                    con_valor_cuota_pactada = table.Column<int>(type: "int", nullable: false),
                    con_tasa_interes = table.Column<int>(type: "int", nullable: false),
                    con_capacidad_sepultura = table.Column<int>(type: "int", nullable: false),
                    con_tipo_compra = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    con_terminos_pago = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    con_nombre_cajero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    con_fecha_primer_vcto_ori = table.Column<DateTime>(type: "datetime2", nullable: false),
                    con_tipo_movimiento = table.Column<int>(type: "int", nullable: false),
                    con_cuotas_pactadas_mod = table.Column<int>(type: "int", nullable: false),
                    con_estado_contrato = table.Column<int>(type: "int", nullable: false),
                    con_num_repactaciones = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contrato", x => x.con_id);
                });

            migrationBuilder.CreateTable(
                name: "IngresosDiferidos",
                columns: table => new
                {
                    ing_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ing_num_con = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ing_precio_base = table.Column<int>(type: "int", nullable: false),
                    ing_nro_cuota = table.Column<int>(type: "int", nullable: false),
                    ing_interes_diferido = table.Column<int>(type: "int", nullable: false),
                    ing_fecha_contab = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ing_estado_contab = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngresosDiferidos", x => x.ing_id);
                });

            migrationBuilder.CreateTable(
                name: "InteresesPorDevengar",
                columns: table => new
                {
                    int_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    int_num_con = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    int_nro_cuota = table.Column<int>(type: "int", nullable: false),
                    int_saldo_inicial = table.Column<int>(type: "int", nullable: false),
                    int_tasa_interes = table.Column<int>(type: "int", nullable: false),
                    int_cuota_final = table.Column<int>(type: "int", nullable: false),
                    int_abono_a_capital = table.Column<int>(type: "int", nullable: false),
                    int_saldo_final = table.Column<int>(type: "int", nullable: false),
                    int_fecha_vcto = table.Column<DateTime>(type: "datetime2", nullable: false),
                    int_fecha_contab = table.Column<DateTime>(type: "datetime2", nullable: false),
                    int_estado_contab = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteresesPorDevengar", x => x.int_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contrato");

            migrationBuilder.DropTable(
                name: "IngresosDiferidos");

            migrationBuilder.DropTable(
                name: "InteresesPorDevengar");
        }
    }
}
