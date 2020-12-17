import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http'
import { environment } from 'src/environments/environment';
import { first } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LpsolverService {

  constructor(private http: HttpClient) { }

  solveLP(lpModelQuery: any, ip: boolean){
    return this.http.post<any>(`${environment.apiUrl}/solve?integerprogramming=${ip}`, lpModelQuery).pipe(first());
  }

  getResult(taskId: any){
    return this.http.post<any>(`${environment.apiUrl}/taskresult?taskId=${taskId}`, {}).pipe(first());
  }

  getTheLast(itemCount: any){
    return this.http.post<any>(`${environment.apiUrl}/historyitems?itemCount=${itemCount}`, {});
  }

  numberOfHistoryItems(){
    return this.http.post<any>(`${environment.apiUrl}/historyitemcount`, {}).pipe(first());
  }
}
