import { Component, OnInit } from '@angular/core';
import { LpsolverService } from '../_services/lpsolver.service';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.css']
})
export class HistoryComponent implements OnInit {

  historyReceived: boolean = false;
  history: any;
  historyItemCount: number = 0;

  constructor(private lpsolverService: LpsolverService) { }

  ngOnInit(): void {
    this.lpsolverService.numberOfHistoryItems()
      .subscribe(response => this.historyItemCount = response.itemCount);
  }

  searchForHistory(): void {
    if(this.numberOfItems() <= 0){
      alert("You have to ask for one history item at least!");
      return;
    }
    else{
      this.historyReceived = false;
      this.lpsolverService.getTheLast(this.numberOfItems())
        .subscribe(history =>{
          this.history = history;
          this.historyReceived = true;
        });
    }
  }

  private numberOfItems(): number {
    return Number((document.getElementById("numberOfItems") as HTMLInputElement).value);
  }
}
