import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { DocumentsResolve } from './document-resolve';

const routes: Routes = [];

// [{
//   path: '',
//   component: AppComponent,
//   resolve: {
//     docs: DocumentsResolve
//   }
// }];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
  // providers: [DocumentsResolve]
})
export class AppRoutingModule { }
