# TaskFlow · Front

## Estructura

```
src/
├── main.tsx                 Arranque: Keycloak → QueryClient → Router
├── app/                     NAVEGACIÓN (TanStack Router)
│   ├── routes.ts            Todas las rutas (públicas, errores y feats)
│   ├── router.tsx           Árbol de rutas + router
│   └── layouts/
│       ├── app/             AppLayout (sidebar + header). La feat activa va en <Outlet />
│       │   └── components/  personalNav.ts = tabs del sidebar
│       └── public/          Login, Register, Callback y páginas de error
├── features/                UNA CARPETA POR PÁGINA
│   └── <feat>/
│       ├── <feat>.tsx       Componente página (estado + queries)
│       └── components/      Componentes solo de esa feat (+ index.ts y types.ts)
├── components/
│   ├── ui/                  shadcn (generado, no editar a mano)
│   ├── common/              Compartido entre feats: popups, drawer de tarea,
│   │                        FilterSelector, Pagination, QueryState, badges...
│   └── index.comp.ts        Barrel: importa todo desde "@/components/index.comp"
├── hooks/                   Hooks de TanStack Query (use-project, use-task, use-users) y use-auth
└── lib/
    ├── api/
    │   ├── client.ts        apiFetch + token + ApiError
    │   ├── endpoints/       UNA función por endpoint del back (projectsApi, tasksApi, usersApi)
    │   └── types/           Tipos de las respuestas del back
    ├── auth/keycloak.ts     login / logout / isAdmin / token
    ├── constants/task.ts    Labels, opciones y colores de estado/prioridad
    ├── query-keys.ts        Claves de caché
    ├── query-client.ts      Configuración global de TanStack Query
    ├── notify.ts            notify.success / notify.error (toasts)
    └── format.ts            formatDate / formatDateTime
```

### ¿Dónde toco...?

| Quiero...                         | Archivo                                                        |
| --------------------------------- | -------------------------------------------------------------- |
| Añadir una página nueva           | `features/<nueva>/` → exportar en `features/index.ts` → ruta en `app/routes.ts` + `app/router.tsx` → tab en `personalNav.ts` |
| Llamar a un endpoint nuevo        | `lib/api/endpoints/*.ts` → hook en `hooks/use-*.ts` → key en `lib/query-keys.ts` |
| Cambiar textos/colores de estado  | `lib/constants/task.ts`                                        |
| Un componente usado en 2+ feats   | `components/common/` y exportarlo en `components/index.comp.ts` |

---

# React + TypeScript + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is enabled on this template. See [this documentation](https://react.dev/learn/react-compiler) for more information.

Note: This will impact Vite dev & build performances.
You can also try [the experimental native React Compiler support in plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react/README.md#rust-react-compiler) by using `compiler: true` in the plugin options instead of using the Babel plugin.

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])

```

You can also install [eslint-plugin-react-x](https://npmx.dev/package/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://npmx.dev/package/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])

```
