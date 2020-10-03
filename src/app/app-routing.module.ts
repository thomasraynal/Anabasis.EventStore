import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { AppDocument } from './app.document';
import { AppSearch } from './app.search';
import { DocumentsResolve } from './document-resolve';

const routes: Routes = [
 { path: '#', component: AppComponent},
 { path: 'search', component: AppSearch},
 { path: ':document', component: AppDocument },
 { path: ':document/:title', component: AppDocument },
 { path: ':document/:title/:subtitle', component: AppDocument }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule { }
