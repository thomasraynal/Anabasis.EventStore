import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HttpClientModule } from '@angular/common/http';
import { AppDocument } from './app.document';
import { AppSearch } from './app.search';
import { AppMenu } from './app.menu';

@NgModule({
  declarations: [
    AppComponent,
    AppDocument,
    AppSearch,
    AppMenu
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
