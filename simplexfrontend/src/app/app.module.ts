import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

import { HomeComponent } from './home/home.component';
import { LpModelCreatorComponent } from './lp-model-creator/lp-model-creator.component';
import { ResultComponent } from './result/result.component';
import { Utils } from './_utils/utils';
import { HistoryComponent } from './history/history.component'

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    LpModelCreatorComponent,
    ResultComponent,
    HistoryComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
  providers: [
    { provide: Utils }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
