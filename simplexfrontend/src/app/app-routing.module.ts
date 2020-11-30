import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent} from './home/home.component';
import { LpModelCreatorComponent } from './lp-model-creator/lp-model-creator.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'solve', component: LpModelCreatorComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
