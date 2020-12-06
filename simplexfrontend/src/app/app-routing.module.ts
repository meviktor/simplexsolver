import { Component, NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HistoryComponent } from './history/history.component';
import { HomeComponent} from './home/home.component';
import { LpModelCreatorComponent } from './lp-model-creator/lp-model-creator.component';
import { ResultComponent } from './result/result.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'solve', component: LpModelCreatorComponent },
  { path: 'result/:id', component: ResultComponent },
  { path: 'history', component: HistoryComponent },
  { path: '*', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
