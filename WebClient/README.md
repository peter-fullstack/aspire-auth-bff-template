## Next.js App Router Course - Starter

This is the starter template for the Next.js App Router Course. It contains the starting code for the dashboard application.

For more information, see the [course curriculum](https://nextjs.org/learn) on the Next.js Website.

## Project notes (review)

- **Tech**: Next.js App Router + React + TypeScript + Tailwind.
- **Data**: direct SQL via the `postgres` library (see `app/lib/data.ts`) plus mutations via Server Actions (see `app/lib/actions.ts`).
- **Auth**: NextAuth/Auth.js v5 (credentials provider) via `auth.ts` + `auth.config.ts`. Login uses a Server Action, logout uses an inline Server Action in the side nav.

### Important caveats

- **Route protection uses Next.js Proxy (`proxy.ts`)**: The lockfile in this repo resolves **Next.js 16** (`next@16.0.10`), where a root `proxy.ts` is the supported request-boundary convention (replacing the older root `middleware.ts` name in current docs). It runs the NextAuth `auth` export with a `matcher`, same idea as the Learn dashboard tutorial. If you **downgrade** to a release that does not implement `proxy.ts`, use a root `middleware.ts` instead. See [Proxy (file convention)](https://nextjs.org/docs/pages/api-reference/file-conventions/proxy) and [Renaming Middleware to Proxy](https://nextjs.org/docs/messages/middleware-to-proxy).
- **Environment**: the code uses `process.env.POSTGRES_URL!` in multiple modules. If `POSTGRES_URL` is missing, you’ll get a runtime crash.
- **Tutorial-only behavior**: `app/lib/data.ts` includes intentional `setTimeout` delays and some `console.log` calls—fine for demos, but remove/guard for real deployments.

## Local development

### Prerequisites

- Node.js + pnpm
- A Postgres database (connection string in `POSTGRES_URL`)

### Environment variables

Create an `.env.local` with:

- `POSTGRES_URL`: Postgres connection string

### Commands

- `pnpm dev`: run locally
- `pnpm build`: production build
- `pnpm lint`: lint

## Detailed technical review

Structured walk-through of the codebase (updated for **Next.js 16** + root `proxy.ts`, and current `package.json`). Use this as a map when extending the template.

### 1) High-level tree (top-level + `app/` key subfolders)

**Top-level (not exhaustive, focused on key items present):**

- `app/`
- `.eslintrc.json`
- `.gitignore`
- `.vscode/launch.json`
- `auth.config.ts`
- `auth.ts`
- `next.config.ts`
- `package.json`
- `pnpm-lock.yaml`
- `postcss.config.js`
- `proxy.ts`
- `README.md`
- `tailwind.config.ts`
- `tsconfig.json`

**`app/` structure (key subfolders/routes):**

- `app/layout.tsx` (root layout)
- `app/page.tsx` (home)
- `app/login/page.tsx` (sign-in page)
- `app/dashboard/`
  - `app/dashboard/layout.tsx`
  - `app/dashboard/(overview)/page.tsx` + `loading.tsx`
  - `app/dashboard/customers/page.tsx`
  - `app/dashboard/invoices/page.tsx`
  - `app/dashboard/invoices/create/page.tsx`
  - `app/dashboard/invoices/[id]/edit/page.tsx` + `not-found.tsx`
  - `app/dashboard/invoices/error.tsx`
- `app/lib/`
  - `actions.ts` (**Server Actions**, includes auth sign-in + invoice mutations)
  - `data.ts` (DB queries)
  - `definitions.ts`, `utils.ts`, `placeholder-data.ts`
- `app/ui/`
  - `app/ui/login-form.tsx`
  - `app/ui/dashboard/*` (nav, cards, chart, etc.)
  - `app/ui/invoices/*`, `app/ui/customers/*`, `app/ui/global.css`, etc.
- Route handlers (not under `app/api/` in this snapshot):
  - `app/query/route.ts` (**route handler**, currently commented out)
  - `app/seed/route.ts` (**route handler**, currently commented out)

### 2) Key libraries from `package.json`

```json
  "dependencies": {
    "@heroicons/react": "^2.2.0",
    "@tailwindcss/forms": "^0.5.10",
    "autoprefixer": "10.4.20",
    "bcrypt": "^5.1.1",
    "clsx": "^2.1.1",
    "next": "latest",
    "next-auth": "5.0.0-beta.25",
    "postcss": "8.5.1",
    "postgres": "^3.4.6",
    "react": "latest",
    "react-dom": "latest",
    "tailwindcss": "3.4.17",
    "typescript": "5.7.3",
    "use-debounce": "^10.0.4",
    "zod": "^3.25.17"
  },
  "devDependencies": {
    "@types/bcrypt": "^5.0.2",
    "@types/node": "22.10.7",
    "@types/react": "19.0.7",
    "@types/react-dom": "19.0.3",
    "eslint": "^8.57.1",
    "eslint-config-next": "^15.5.15"
  },
  "pnpm": {
    "onlyBuiltDependencies": ["bcrypt", "sharp"]
  }
```

**Highlights:**

- **Next.js App Router** (`next` — lockfile currently pins **16.0.10**)
- **Auth.js / NextAuth v5 beta** (`next-auth@5.0.0-beta.25`)
- **Postgres driver** (`postgres`) used directly from server code
- **Credentials auth** via `bcrypt` + SQL user table
- **Server Actions validation** via `zod`
- **Tailwind UI** (`tailwindcss`, `@tailwindcss/forms`, `@heroicons/react`)

### 3) How auth is implemented (NextAuth/Auth.js, boundary protection, config)

#### Core auth setup

Auth is configured in **`auth.ts`** using **NextAuth v5** with the **Credentials provider** (Zod-validated email/password, `bcrypt.compare`, user loaded from Postgres):

```1:44:auth.ts
import NextAuth from 'next-auth';
import Credentials from 'next-auth/providers/credentials';
import { authConfig } from './auth.config';
import { z } from 'zod';
import type { User } from '@/app/lib/definitions';
import bcrypt from 'bcrypt';
import postgres from 'postgres';

const sql = postgres(process.env.POSTGRES_URL!, { ssl: 'require' });

async function getUser(email: string): Promise<User | undefined> {
  try {
    const user = await sql<User[]>`SELECT * FROM users WHERE email=${email}`;
    return user[0];
  } catch (error) {
    console.error('Failed to fetch user:', error);
    throw new Error('Failed to fetch user.');
  }
}

export const { auth, signIn, signOut } = NextAuth({
  ...authConfig,
  providers: [
    Credentials({
      async authorize(credentials) {
        const parsedCredentials = z
          .object({ email: z.string().email(), password: z.string().min(6) })
          .safeParse(credentials);

        if (parsedCredentials.success) {
          const { email, password } = parsedCredentials.data;
          const user = await getUser(email);

          if (!user) return null;

          const passwordsMatch = await bcrypt.compare(password, user.password);
          if (passwordsMatch) return user;
        }

        return null;
      },
    }),
  ],
});
```

#### Route protection / redirects

The config in **`auth.config.ts`** uses the v5 `callbacks.authorized` gate:

```1:21:auth.config.ts
import type { NextAuthConfig } from 'next-auth';

export const authConfig = {
  pages: {
    signIn: '/login',
  },
  callbacks: {
    authorized({ auth, request: { nextUrl } }) {
      const isLoggedIn = !!auth?.user;
      const isOnDashboard = nextUrl.pathname.startsWith('/dashboard');
      if (isOnDashboard) {
        if (isLoggedIn) return true;
        return false; // Redirect unauthenticated users to login page
      } else if (isLoggedIn) {
        return Response.redirect(new URL('/dashboard', nextUrl));
      }
      return true;
    },
  },
  providers:[]
} satisfies NextAuthConfig;
```

So:

- Unauthenticated users visiting `/dashboard*` are **blocked** (the auth boundary redirects to `/login` because `pages.signIn` is set).
- Logged-in users visiting non-dashboard pages get redirected **to `/dashboard`**.

#### Request boundary: `proxy.ts` (not `middleware.ts` in this template)

There is **no root `middleware.ts`** in this repo. Root **`proxy.ts`** exports `NextAuth(authConfig).auth` plus a `matcher`. On **Next.js 16**, this is the **supported** file name for that boundary (see Next.js docs linked in **Important caveats** above). It is **not** dead code merely because the file is not named `middleware.ts`.

```1:9:proxy.ts
import NextAuth from 'next-auth';
import { authConfig } from './auth.config';

export default NextAuth(authConfig).auth;

export const config = {
  // https://nextjs.org/docs/app/api-reference/file-conventions/proxy#matcher
  matcher: ['/((?!api|_next/static|_next/image|.*\\.png$).*)'],
};
```

#### Login + logout wiring

- Login uses a **Server Action** `authenticate` that calls `signIn('credentials', formData)`:

```1:27:app/lib/actions.ts
"use server";

import { z } from "zod";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import postgres from "postgres";
import { signIn } from '@/auth';
import { AuthError } from 'next-auth';

export async function authenticate(
  prevState: string | undefined,
  formData: FormData,
) {
  try {
    await signIn('credentials', formData);
  } catch (error) {
    if (error instanceof AuthError) {
      switch (error.type) {
        case 'CredentialsSignin':
          return 'Invalid credentials.';
        default:
          return 'Something went wrong.';
      }
    }
    throw error;
  }
}
```

- The login form is client-side and uses `useActionState(authenticate, ...)`:

```15:24:app/ui/login-form.tsx
export default function LoginForm() {
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/dashboard";
  const [errorMessage, formAction, isPending] = useActionState(
    authenticate,
    undefined,
  );

  return (
    <form action={formAction} className="space-y-3">
      {/* ... */}
    </form>
  );
}
```

- Logout is wired from the dashboard sidenav via an inline server action calling `signOut`:

```21:29:app/ui/dashboard/sidenav.tsx
        <form action={async () => {
            'use server';
            await signOut({ redirectTo: '/' });
          }}>
          <button className="flex h-[48px] w-full grow items-center justify-center gap-2 rounded-md bg-gray-50 p-3 text-sm font-medium hover:bg-sky-100 hover:text-blue-600 md:flex-none md:justify-start md:p-2 md:px-3">
            <PowerIcon className="w-6" />
            <div className="hidden md:block">Sign Out</div>
          </button>
        </form>
```

### 4) API routes or Server Actions usage

#### Server Actions

Yes — the project uses Server Actions heavily:

- **`app/lib/actions.ts`** starts with `"use server";` and defines:
  - `authenticate` (login)
  - `createInvoice`, `updateInvoice`, `deleteInvoice` (mutations)
  - Uses `revalidatePath()` + `redirect()` for navigation/cache invalidation

There is also an **inline server action** in `app/ui/dashboard/sidenav.tsx` for sign-out.

#### Route handlers (“API routes”)

There are route handlers, but **not under `app/api/`** in this snapshot:

- `app/query/route.ts`
- `app/seed/route.ts`

Both currently have their `GET` handlers and DB code **commented out** (so they do not serve active endpoints until uncommented).

### 5) Notable risks and footguns

- **`proxy.ts` vs `middleware.ts`**: Prefer verifying redirects with `pnpm dev` / `pnpm build`. If you move to an older Next.js line without `proxy.ts`, switch to root `middleware.ts` with the same default export pattern (see Next.js migration note above).
- **Hard non-null assertions on `POSTGRES_URL`**: multiple files use `process.env.POSTGRES_URL!` (for example `auth.ts`, `app/lib/actions.ts`, `app/lib/data.ts`). If the variable is missing, you get a runtime crash rather than a controlled failure.
- **DB connections instantiated in multiple modules**: `postgres(process.env.POSTGRES_URL!, ...)` appears in several files. In serverless or highly parallel runtimes, be deliberate about connection reuse and pooling.
- **Artificial `setTimeout` delays in data fetching**: `app/lib/data.ts` intentionally sleeps (tutorial pacing). Remove for production workloads.
- **Verbose server logging**: `console.log` and similar in `app/lib/data.ts` can leak data or add noise in production.
- **NextAuth v5 beta**: `next-auth@5.0.0-beta.25` can change across upgrades; pin and test when bumping versions.
