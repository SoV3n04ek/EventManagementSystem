import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';

const LOGIN_COMPONENT = () => import('./features/auth/login/login').then(m => m.LoginComponent);
const REGISTER_COMPONENT = () => import('./features/auth/register/register').then(m => m.RegisterComponent);
const EVENT_LIST_COMPONENT = () => import('./features/events/event-list/event-list').then(m => m.EventListComponent);
const EVENT_CREATE_COMPONENT = () => import('./features/events/event-create/event-create').then(m => m.EventCreateComponent);
const EVENT_DETAIL_COMPONENT = () => import('./features/events/event-detail/event-detail').then(m => m.EventDetailComponent);
const MY_EVENTS_COMPONENT = () => import('./features/calendar/my-events/my-events').then(m => m.MyEventsComponent);

export const routes: Routes = [
   {
    path: '',
    redirectTo: '/events',
    pathMatch: 'full'    
   },
   {
    path: 'login',
    loadComponent: LOGIN_COMPONENT
   },
   {
    path: 'register',
    loadComponent: REGISTER_COMPONENT
   },
   {
    path: 'events',
    children: [
        {
            path: '',
            loadComponent: EVENT_LIST_COMPONENT
        },
        {
            path: 'create',
            loadComponent: EVENT_CREATE_COMPONENT,
            canActivate: [authGuard]
        },
        {
            path: ':id',
            loadComponent: EVENT_DETAIL_COMPONENT
        },
        {
            path: ':id/edit',
            loadComponent: EVENT_CREATE_COMPONENT,
            canActivate: [authGuard],
            data: { isEdit: true }
        }
    ]
   },
   {
    path: 'my-events',
    loadComponent: MY_EVENTS_COMPONENT,
    canActivate: [authGuard]
   },
   {
    path: '**',
    redirectTo: '/events'
   }
]