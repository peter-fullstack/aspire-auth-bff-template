// This route is intentionally disabled in the starter/template.
// Keeping a real module export avoids Vercel/Next typecheck failures
// when the tutorial code below is commented out.
export async function GET() {
  return Response.json(
    { message: 'Disabled. Uncomment `app/query/route.ts` to enable.' },
    { status: 404 },
  );
}

// import postgres from 'postgres';
//
// const sql = postgres(process.env.POSTGRES_URL!, { ssl: 'require' });
//
// async function listInvoices() {
// 	const data = await sql`
//     SELECT invoices.amount, customers.name
//     FROM invoices
//     JOIN customers ON invoices.customer_id = customers.id
//     WHERE invoices.amount >= 100;
//   `;
//
// 	return data;
// }
//
// export async function GET() {
//   try {
//   	return Response.json(await listInvoices());
//   } catch (error) {
//   	return Response.json({ error }, { status: 500 });
//   }
// }
