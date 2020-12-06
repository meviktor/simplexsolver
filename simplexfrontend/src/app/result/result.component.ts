import { Component, OnInit } from '@angular/core';
import { Console } from 'console';
import { LpsolverService } from '../_services/lpsolver.service';
import { Utils } from '../_utils/utils';

@Component({
  selector: 'app-result',
  templateUrl: './result.component.html',
  styleUrls: ['./result.component.css']
})
export class ResultComponent implements OnInit {

  private taskId: any = window.location.href.split("/").pop();
  initialized: boolean = false;
  lpTaskResult: any;
  utils: Utils;
  possibleSideConnections: string[] = ["<=", "=", ">="]
  showAsFractions: boolean = false;

  constructor(private lpsolverService: LpsolverService, utils: Utils) {
    this.utils = utils;
  }

  ngOnInit(): void {
    this.lpsolverService.getResult(this.taskId)
      .subscribe(result => {
        this.lpTaskResult = result;
        this.initialized = true;
      });
  }

  fractionForm(numerator: number, denominator: number): string{
    return this.sameSigned(numerator, denominator) ? `${Math.abs(numerator)} / ${Math.abs(denominator)}` : `${numerator} / ${denominator}`;
  }

  decimalForm(numerator: number, denominator: number): string{
    return (numerator / denominator).toString();
  }

  showAsFractionChanged(){
    this.showAsFractions = !this.showAsFractions;
  }

  private sameSigned(numerator: number, denominator: number): boolean{
    return (numerator >= 0 && denominator > 0) || (numerator < 0 && denominator < 0);
  }
}
